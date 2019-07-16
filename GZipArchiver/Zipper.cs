using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipArchiver
{
    public static class Zipper
    {
//        private static int breakAfterProcessingFailuresAttempts = 2;

        public static void Process(
            string fileName,
            SegmentProvider segmentProvider,
            CompressionMode mode, 
            long maxCollectionSize, 
            DataContext dataContext,
            SynchronizationContext synchronizationContext)
        {
            using (var inputStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                while (true)
                {
                    if (synchronizationContext.FinishedReading)
                    {
                        break;
                    }

                    try
                    {
                        var indexAndSize = segmentProvider.GetNextIndexAndSize(inputStream);
                        var index = indexAndSize.Item1;
                        var size = indexAndSize.Item2;

                        if (size == 0)
                        {
                            synchronizationContext.FinishedReading = true;
                            dataContext.OutputData[index] = new Segment(new byte[0], true);
                            break;
                        }

                        Trace.TraceInformation(
                            $"Worker {Thread.CurrentThread.GetHashCode()} processing {index} segment.");
                        var buffer = new byte[size];
                        inputStream.Read(buffer, 0, size);

                        dataContext.OutputData[index] = mode == CompressionMode.Compress
                            ? Compress(buffer)
                            : Decompress(buffer);
                        synchronizationContext.WriterEvent.Set();
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($" Error: {e.Message}");
                        throw;

//                        if (segment.RetryCount++ >= breakAfterProcessingFailuresAttempts)
//                            throw; // Critical error, stoppingprocessing
//
//                        // return segment back for processing
//                        dataContext.InputData[index] = segment;
                    }
                }
            }

            Trace.TraceInformation($"Worker {Thread.CurrentThread.GetHashCode()} stopped processing data.");
        }

        private static Segment Compress(byte[] data)
        {
            using (var outMemoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(outMemoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(data, 0, data.Length);
                }
                return new Segment(outMemoryStream.ToArray());
            }
        }

        private static Segment Decompress(byte[] data)
        {
            using (var inMemoryStream = new MemoryStream(data))
            {
                using (var gZipStream = new GZipStream(inMemoryStream, CompressionMode.Decompress))
                using (var outMemoryStream = new MemoryStream())
                {
                    int read;
                    var buffer = new byte[data.Length];

                    while ((read = gZipStream.Read(buffer, 0, data.Length)) != 0)
                    {
                        outMemoryStream.Write(buffer, 0, read);
                    }

                    return new Segment(outMemoryStream.ToArray());
                }
            }
        }
    }
}
﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipArchiver
{
    public static class Zipper
    {
        private static int breakAfterProcessingFailuresAttempts = 2;

        public static void Process(
            CompressionMode mode, 
            long maxCollectionSize, 
            DataContext dataContext,
            SynchronizationContext synchronizationContext)
        {
            while (true)
            {
                synchronizationContext.ZipperEvent.WaitOne();
                var inputDataKeys = dataContext.InputData.Keys;
                if (!inputDataKeys.Any() && synchronizationContext.FinishedReading)
                {
                    synchronizationContext.ZipperEvent.Set();
                    break;
                }

                if (!inputDataKeys.Any() || dataContext.OutputData.Keys.Count() >= maxCollectionSize)
                {
                    synchronizationContext.ZipperEvent.Reset();
                    continue;
                }

                Segment segment;
                var index = inputDataKeys.Min();
                if (dataContext.InputData.TryRemove(index, out segment))
                {
                    try
                    {
                        Trace.TraceInformation($"Worker {Thread.CurrentThread.GetHashCode()} processing {index} segment.");
                        dataContext.OutputData[index] = mode == CompressionMode.Compress ? Compress(segment) : Decompress(segment);
                        segment.Data = null;
                        synchronizationContext.ReaderEvent.Set();
                        synchronizationContext.WriterEvent.Set();
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Segment number: {index}; Retry attempt: {segment.RetryCount}; Error: {e.Message}");

                        if (segment.RetryCount++ >= breakAfterProcessingFailuresAttempts)
                            throw; // Critical error, stopping processing

                        // return segment back for processing
                        dataContext.InputData[index] = segment;
                    }
                }
            }

            Trace.TraceInformation($"Worker {Thread.CurrentThread.GetHashCode()} stopped processing data.");
        }

        private static Segment Compress(Segment segment)
        {
            using (var outMemoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(outMemoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(segment.Data, 0, segment.Data.Length);
                }
                return new Segment(outMemoryStream.ToArray(), segment.IsFinal);
            }
        }

        private static Segment Decompress(Segment segment)
        {
            using (var inMemoryStream = new MemoryStream(segment.Data))
            {
                using (var gZipStream = new GZipStream(inMemoryStream, CompressionMode.Decompress))
                using (var outMemoryStream = new MemoryStream())
                {
                    int read;
                    var buffer = new byte[segment.Data.Length];

                    while ((read = gZipStream.Read(buffer, 0, segment.Data.Length)) != 0)
                    {
                        outMemoryStream.Write(buffer, 0, read);
                    }

                    return new Segment(outMemoryStream.ToArray(), segment.IsFinal);
                }
            }
        }
    }
}
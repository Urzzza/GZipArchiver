using System;
using System.Diagnostics;
using System.IO;

namespace GZipTest
{
    public static class Writer
    {
        private static int breakAfterWriteFailuresAttempts = 2;

        public static void Write(
            FileStream outputStream,
            bool shouldWriteSegmentSize, 
            DataContext dataContext,
            SynchronizationContext synchronizationContext)
        {
            var segmentNumber = 0;

            while (true)
            {
                synchronizationContext.WriterEvent.WaitOne();
                Segment segment;
                if (dataContext.OutputData.TryRemove(segmentNumber, out segment))
                {
                    try
                    {
                        Trace.TraceInformation($"Writing segment {segmentNumber}; Size: {segment.Data.Length}.");
                        if (shouldWriteSegmentSize)
                        {
                            outputStream.Write(BitConverter.GetBytes(segment.Data.Length), 0, sizeof(int));
                        }

                        outputStream.Write(segment.Data, 0, segment.Data.Length);
                        segment.Data = null;
                        synchronizationContext.ZipperEvent.Set();

                        if (segment.IsFinal)
                        {
                            break;
                        }

                        segmentNumber++;
                        continue;
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError($"Error while writing data; Segment {segmentNumber}; {e.Message}");

                        if (segment.RetryCount++ >= breakAfterWriteFailuresAttempts)
                            throw; // Critical error, stopping processing

                        // return segment back for processing
                        dataContext.OutputData[segmentNumber] = segment;
                        continue;
                    }
                }

                synchronizationContext.WriterEvent.Reset();
            }
        }
    }
}
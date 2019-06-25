using System;
using System.IO;

namespace GZipTest
{
    public static class Writer
    {
        private static int breakAfterWriteFailuresAttempts = 2;

        public static void Write(
            SafeDictionary<int, Segment> sourceDict, 
            FileStream outputStream,
            bool shouldWriteSegmetSize, 
            SynchronizationContext synchronizationContext)
        {
            var segmentNumber = 0;

            while (true)
            {
                synchronizationContext.WriterEvent.WaitOne();
                Segment segment;
                if (sourceDict.TryRemove(segmentNumber, out segment))
                {
                    try
                    {
                        Console.WriteLine($"Writing segment {segmentNumber}; Size: {segment.Data.Length}.");
                        if (shouldWriteSegmetSize)
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
                        Console.WriteLine($"Error while writing data; Segment {segmentNumber}; {e.Message}");

                        if (segment.RetryCount++ >= breakAfterWriteFailuresAttempts)
                            throw; // Critical error, stopping processing

                        // return segment back for processing
                        sourceDict[segmentNumber] = segment;
                        continue;
                    }
                }

                synchronizationContext.WriterEvent.Reset();
            }
        }
    }
}
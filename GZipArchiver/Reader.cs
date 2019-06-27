using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GZipArchiver
{
    public static class Reader
    {
        private static int breakAfterReadFailuresAttempts = 2;

        public static void Read(
            FileStream inputFile,
            bool shouldReadSegmentSize, 
            long bufferSize, 
            long maxCollectionMembers,
            DataContext dataContext,
            SynchronizationContext synchronizationContext)
        {
            var memoryCounter = new PerformanceCounter("Memory", "Available Bytes");
            var segmentNumber = 0;
            var retryCount = 0;
            var segmentSize = CalculateSegmentSize(inputFile, shouldReadSegmentSize, bufferSize);
            while (segmentSize != 0)
            {
                if (dataContext.InputData.Keys.Count() >= maxCollectionMembers || (long)memoryCounter.NextValue() < 2 * bufferSize)
                {
                    Trace.TraceWarning($"Almost out of memory. Waiting to read {segmentNumber} segment.");
                    synchronizationContext.ReaderEvent.Reset();
                    synchronizationContext.ReaderEvent.WaitOne();
                    continue;
                }

                var currentPosition = inputFile.Position;
                var currentSegmentSize = segmentSize;
                byte[] buffer;
                try
                {
                    buffer = new byte[segmentSize];
                    inputFile.Read(buffer, 0, segmentSize);
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Error while reading file, trying to retry; Segment {segmentNumber}; {e.Message}");

                    if (retryCount++ >= breakAfterReadFailuresAttempts)
                        throw; // Critical error, stopping processing

                    inputFile.Seek(currentPosition, SeekOrigin.Begin);
                    continue;
                }

                try
                {
                    segmentSize = CalculateSegmentSize(inputFile, shouldReadSegmentSize, bufferSize);
                    dataContext.InputData[segmentNumber] = new Segment(buffer, segmentSize == 0);
                    synchronizationContext.ZipperEvent.Set();
                }
                catch (Exception e)
                {
                    Trace.TraceError($"Error while saving data from file, trying to retry; Segment {segmentNumber}; {e.Message}");

                    if (retryCount++ >= breakAfterReadFailuresAttempts)
                        throw; // Critical error, stopping processing

                    inputFile.Seek(currentPosition, SeekOrigin.Begin);
                    segmentSize = currentSegmentSize;
                    continue;
                }

                segmentNumber++;
                retryCount = 0;
            }
            synchronizationContext.FinishedReading = true;
            Trace.TraceInformation("Reader finished processing file.");
        }

        private static int CalculateSegmentSize(Stream stream, bool shouldReadSegmentSize, long bufferSize)
        {
            var segmentSize = stream.Length - stream.Position <= bufferSize
                    ? (int) (stream.Length - stream.Position)
                    : (int) bufferSize;
            if (!shouldReadSegmentSize || segmentSize == 0)
            {
                return segmentSize;
            }
            var size = new byte[sizeof(int)];
            stream.Read(size, 0, sizeof(int));
            return BitConverter.ToInt32(size, 0);
        }
    }
}
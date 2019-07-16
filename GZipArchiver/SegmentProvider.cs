using System;
using System.IO;

namespace GZipArchiver
{
    public class SegmentProvider
    {
        private readonly bool shouldReadSegmentSize;
        private readonly long bufferSize;
        private readonly object lockObj = new object();


        private long currentOffset = 0;
        private int currentSegmentIndex = 0;

        public SegmentProvider(
            bool shouldReadSegmentSize,
            long bufferSize)
        {
            this.shouldReadSegmentSize = shouldReadSegmentSize;
            this.bufferSize = bufferSize;
        }

        public Tuple<int, int> GetNextIndexAndSize(Stream stream)
        {
            int segmentSize;
            int segmentIndex;
            lock (lockObj)
            {
                stream.Position = currentOffset;
                segmentSize = CalculateSegmentSize(stream);
                segmentIndex = currentSegmentIndex;
                currentSegmentIndex++;
                currentOffset = currentOffset + segmentSize + (shouldReadSegmentSize ? sizeof(int) : 0);
            }

            return new Tuple<int, int>(segmentIndex, segmentSize);
        }

        private int CalculateSegmentSize(Stream stream)
        {
            if (shouldReadSegmentSize)
            {
                var size = new byte[sizeof(int)];
                stream.Read(size, 0, sizeof(int));
                return BitConverter.ToInt32(size, 0);
            }

            return stream.Length - stream.Position <= bufferSize
                ? (int)(stream.Length - stream.Position)
                : (int)bufferSize;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{
    using System.Diagnostics;

    public class FileProcessor : IDisposable
    {
        private readonly FileStream inputStream;
        private readonly FileStream outputStream;
        private SafeDictionary<int, Segment> inputData = new SafeDictionary<int, Segment>();
        private SafeDictionary<int, Segment> outputData = new SafeDictionary<int, Segment>();
        private List<Thread> workerThreads;
        private Thread readerThread;
        private Thread writerThread;

        private readonly CompressionMode mode;
        private readonly int threadLimit = Environment.ProcessorCount < 3 ? 1 : Environment.ProcessorCount - 2;

        public FileProcessor(FileStream inputStream, FileStream outputStream, CompressionMode mode)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
            this.mode = mode;

            workerThreads = new List<Thread>(threadLimit);
        }

        public void Process()
        {
            var bufferSize = Math.Min(inputStream.Length, 256 * 1024 * 1024); // file size or 256 mb
            var freeMemory = (long)new PerformanceCounter("Memory", "Available Bytes").NextValue();
            if (!Environment.Is64BitProcess)
            {
                /// https://blogs.msdn.microsoft.com/webtopics/2009/05/22/troubleshooting-system-outofmemoryexceptions-in-asp-net/
                /// > Your process is using a lot of memory (typically over 800MB.)
                /// >The virtual address space is fragmented, reducing the likelihood that a large, contiguous allocation will succeed.
                /// 
                /// Reducing memory and buffer size for x86. No optimal memory usage can be achived on x86 and current RAM sizes
                freeMemory = Math.Min(freeMemory, 800 * 1024 * 1024);
                bufferSize = Math.Min(bufferSize, 32 * 1024 * 1024);  // file size or 32 mb
            }

            var availableMemoryForCollection = freeMemory * 0.40; // managing 2 collections, so 100% - 20% (for safety) / 2

            var maxCollectionMembers = (long)availableMemoryForCollection / bufferSize;
            if (maxCollectionMembers < 1)
            {
                maxCollectionMembers = 1;
                bufferSize = (long)Math.Min(inputStream.Length, availableMemoryForCollection);
            }

            var synchronizationContext = new SynchronizationContext();

            readerThread = new Thread(() => Reader.Read(inputData, inputStream, mode == CompressionMode.Decompress, bufferSize, maxCollectionMembers, synchronizationContext));
            readerThread.Start();

            for (var i = 0; i < threadLimit; i++)
            {
                var workerThread = new Thread(() => Zipper.Process(inputData, outputData, mode, maxCollectionMembers, synchronizationContext));
                workerThread.Start();
                workerThreads.Add(workerThread);
            }

            writerThread = new Thread(() => Writer.Write(outputData, outputStream, mode == CompressionMode.Compress, synchronizationContext));
            writerThread.Start();
            writerThread.Join();
        }

        public void Dispose()
        {
            readerThread?.Abort();
            writerThread?.Abort();
            workerThreads.ForEach(x => x?.Abort());
        }
    }
}
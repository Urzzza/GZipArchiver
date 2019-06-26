using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest
{


    public class GZipArchiver : IDisposable
    {
        private const int DefaultBufferSize = 32 * 1024 * 1024; 

        private readonly FileStream inputStream;
        private readonly FileStream outputStream;
        private readonly CompressionMode mode;
        private readonly int threadLimit = Environment.ProcessorCount < 3 ? 1 : Environment.ProcessorCount - 2;

        private List<Thread> workerThreads;
        private Thread readerThread;
        private Thread writerThread;
 
        public GZipArchiver(FileStream inputStream, FileStream outputStream, CompressionMode mode)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
            this.mode = mode;

            workerThreads = new List<Thread>(threadLimit);
        }

        public void Process()
        {
            var bufferSize = Math.Min(inputStream.Length, DefaultBufferSize); // file size or buffer size
            var freeMemory = (long)new PerformanceCounter("Memory", "Available Bytes").NextValue();
            if (!Environment.Is64BitProcess)
            {
                /// https://blogs.msdn.microsoft.com/webtopics/2009/05/22/troubleshooting-system-outofmemoryexceptions-in-asp-net/
                /// Reducing memory and buffer size for x86. No optimal memory usage can be achived on x86 and current RAM sizes
                freeMemory = Math.Min(freeMemory, 800 * 1024 * 1024);
                bufferSize = Math.Min(bufferSize, DefaultBufferSize);  // file size or 1 mb
            }

            var availableMemoryForCollection = freeMemory * 0.40; // managing 2 collections, so 100% - 20% (for safety) / 2

            var maxCollectionMembers = (long)availableMemoryForCollection / bufferSize;
            if (maxCollectionMembers < 1)
            {
                maxCollectionMembers = 1;
                bufferSize = (long)Math.Min(inputStream.Length, availableMemoryForCollection);
            }

            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();

            readerThread = new Thread(
                () => 
                    Reader.Read(
                        inputStream, 
                        mode == CompressionMode.Decompress, 
                        bufferSize, 
                        maxCollectionMembers,
                        dataContext,
                        synchronizationContext));
            readerThread.Start();

            for (var i = 0; i < threadLimit; i++)
            {
                var workerThread = new Thread(
                    () => 
                        Zipper.Process(
                            mode, 
                            maxCollectionMembers, 
                            dataContext,
                            synchronizationContext));
                workerThread.Start();
                workerThreads.Add(workerThread);
            }

            writerThread = new Thread(
                () => 
                    Writer.Write(
                        outputStream, 
                        mode == CompressionMode.Compress, 
                        dataContext,
                        synchronizationContext));
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
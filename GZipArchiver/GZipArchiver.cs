using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipArchiver
{
    public class GZipArchiver : IDisposable
    {
        private readonly FileStream inputStream;
        private readonly FileStream outputStream;
        private readonly CompressionMode mode;
        private readonly int workerThreadLimit = Environment.ProcessorCount < 3 ? 1 : Environment.ProcessorCount - 2;

        private List<Thread> workerThreads;
        private Thread readerThread;
        private Thread writerThread;
 
        public GZipArchiver(FileStream inputStream, FileStream outputStream, CompressionMode mode)
        {
            this.inputStream = inputStream;
            this.outputStream = outputStream;
            this.mode = mode;

            workerThreads = new List<Thread>(workerThreadLimit);
        }

        public void Process()
        {
            var freeMemory = (long)new PerformanceCounter("Memory", "Available Bytes").NextValue();
            if (!Environment.Is64BitProcess)
            {
                /// https://blogs.msdn.microsoft.com/webtopics/2009/05/22/troubleshooting-system-outofmemoryexceptions-in-asp-net/
                /// Reducing memory and buffer size for x86. No optimal memory usage can be achived on x86 and current RAM sizes
                freeMemory = Math.Min(freeMemory, 800 * 1024 * 1024);
            }

            var bufferSize = Math.Min(freeMemory / workerThreadLimit / 2, inputStream.Length / workerThreadLimit);
            bufferSize = Math.Min(bufferSize, 256 * 1024 * 1024); // reading/writing more than 256 produces more delays
            var availableMemoryForCollection = freeMemory * 0.45; // managing 2 collections, so 100% - 10% (for safety) / 2

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

            for (var i = 0; i < workerThreadLimit; i++)
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
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
        private readonly string inputFileName;
        private readonly string outputFileName;
        private readonly CompressionMode mode;
        private readonly int workerThreadLimit = Environment.ProcessorCount;

        private List<Thread> workerThreads;
        private Thread writerThread;
        
        public GZipArchiver(string inputFileName, string outputFileName, CompressionMode mode)
        {
            this.inputFileName = inputFileName;
            this.outputFileName = outputFileName;
            this.mode = mode;

            workerThreads = new List<Thread>(workerThreadLimit);
        }

        public void Process()
        {
            var freeMemory = (long)new PerformanceCounter("Memory", "Available Bytes").NextValue();
            if (!Environment.Is64BitProcess)
            {
                Trace.TraceInformation($"x86, Free memory: {freeMemory}");
                /// https://blogs.msdn.microsoft.com/webtopics/2009/05/22/troubleshooting-system-outofmemoryexceptions-in-asp-net/
                /// Reducing memory and buffer size for x86. No optimal memory usage can be achived on x86 and current RAM sizes
                freeMemory = Math.Min(freeMemory, 800 * 1024 * 1024);
            }

            //var bufferSize = Math.Min(freeMemory / workerThreadLimit / 2, inputFileName.Length / workerThreadLimit + workerThreadLimit);
            var bufferSize = 4L * 1024 * 1024;
            Trace.TraceInformation($"Free memory: {freeMemory}; Worker limit: {workerThreadLimit}; Buffer size: {bufferSize}");

            bufferSize = Math.Min(bufferSize, 256 * 1024 * 1024); // reading/writing more than 256 produces more delays
            var availableMemoryForCollection = freeMemory * 0.9; // managing 2 collections, so 100% - 10% (for safety) / 2
            Trace.TraceInformation($"Available Memory For Collection: {availableMemoryForCollection}; Buffer size: {bufferSize}");

            var maxCollectionMembers = (long)availableMemoryForCollection / bufferSize;
            if (maxCollectionMembers < 1)
            {
                maxCollectionMembers = 1;
                bufferSize = (long)Math.Min(inputFileName.Length, availableMemoryForCollection);
            }

            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();
            var segmentProvider = new SegmentProvider(mode == CompressionMode.Decompress, bufferSize);
           
            for (var i = 0; i < workerThreadLimit; i++)
            {
                var workerThread = new Thread(
                    () => 
                        Zipper.Process(
                            inputFileName,
                            segmentProvider,
                            mode, 
                            maxCollectionMembers, 
                            dataContext,
                            synchronizationContext));
                workerThread.Start();
                workerThreads.Add(workerThread);
            }

            using (var outStream = new FileStream(this.outputFileName, FileMode.Create))
            {
                writerThread = new Thread(
                    () =>
                        Writer.Write(
                            outStream,
                            mode == CompressionMode.Compress,
                            dataContext,
                            synchronizationContext));
                writerThread.Start();
                writerThread.Join();
            }
        }

        public void Dispose()
        {
            writerThread?.Abort();
            workerThreads.ForEach(x => x?.Abort());
        }
    }
}
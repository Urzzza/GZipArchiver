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
            freeMemory = Math.Min(freeMemory, 800 * 1024 * 1024); // 800Mb is already 200 segments of data in queue, zippers should be stopped

            var bufferSize = 4L * 1024 * 1024;
            Trace.TraceInformation($"Free memory: {freeMemory}; Worker limit: {workerThreadLimit}; Buffer size: {bufferSize}");

            var maxCollectionMembers = freeMemory / bufferSize;
            if (maxCollectionMembers < 1) // rare case
            {
                maxCollectionMembers = 1;
                bufferSize = Math.Min(inputFileName.Length, freeMemory);
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
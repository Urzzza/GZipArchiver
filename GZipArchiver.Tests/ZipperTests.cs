using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipArchiver.Tests
{
    [TestClass]
    public class ZipperTests
    {
        [TestMethod]
        public void TestZipper()
        {
            var fileSize = 1024 * 1024;
            var bufferSize = 32 * 1024;
            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();
            FileGenerator.CreateOrReadCyclicFile(fileSize);
            var fileName = FileGenerator.GetInputFileName(fileSize);
            var segmentProvider = new SegmentProvider(false, bufferSize);

            Zipper.Process(fileName, segmentProvider, CompressionMode.Compress, 10, dataContext, synchronizationContext);
            Assert.AreEqual(fileSize / bufferSize + 1, dataContext.OutputData.Keys.Count());
        }

        [TestMethod]
        public void TestZipper_Concurrency()
        {
            var fileSize = 1024 * 1024;
            var bufferSize = 32 * 1024;
            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();
            FileGenerator.CreateOrReadCyclicFile(fileSize);
            var fileName = FileGenerator.GetInputFileName(fileSize);
            var segmentProvider = new SegmentProvider(false, bufferSize);
            List<Thread> workerThreads = new List<Thread>();
            for (var i = 0; i < 3; i++)
            {
                var workerThread = new Thread(
                    () =>
                        Zipper.Process(
                            fileName,
                            segmentProvider,
                            CompressionMode.Compress,
                            10,
                            dataContext,
                            synchronizationContext));
                workerThread.Start();
                workerThreads.Add(workerThread);
            }

            foreach (var workerThread in workerThreads)
            {
                workerThread.Join();
            }

            var lastElementIndex = fileSize / bufferSize;
            Assert.AreEqual(lastElementIndex + 1, dataContext.OutputData.Keys.Count());
            Assert.AreEqual(0, dataContext.OutputData[lastElementIndex].Data.Length);
        }
    }
}
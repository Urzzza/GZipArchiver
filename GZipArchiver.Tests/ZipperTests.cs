using System;
using System.IO.Compression;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipArchiver.Tests
{
    [TestClass]
    public class ZipperTests
    {
        [TestMethod]
        public void TestZipper()
        {
            var itemsCount = 100;
            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();
            var rand = new Random();

            var buffer = new byte[4 * 1024];
            rand.NextBytes(buffer);
            for (int i = 0; i < itemsCount; i++)
            {
                dataContext.InputData[i] = new Segment(buffer, i == itemsCount - 1);
            }

            synchronizationContext.FinishedReading = true;
            synchronizationContext.ZipperEvent.Set();

            Zipper.Process(CompressionMode.Compress, itemsCount + 1, dataContext, synchronizationContext);
            Assert.AreEqual(itemsCount, dataContext.OutputData.Keys.Count());
            Assert.AreEqual(0, dataContext.InputData.Keys.Count());
        }
    }
}
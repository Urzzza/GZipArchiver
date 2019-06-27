using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipArchiver.Tests
{
    [TestClass]
    public class WriterTests
    {
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TestWriter(bool shouldWriteSegmentSize)
        {
            var itemsCount = 100;
            var fileName = "writerDecompress.test";
            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();
            var rand = new Random();

            var buffer = new byte[4 * 1024];
            rand.NextBytes(buffer);
            for (int i = 0; i < itemsCount; i++)
            {
                dataContext.OutputData[i] = new Segment(buffer, i == itemsCount - 1);
            }
            
            synchronizationContext.WriterEvent.Set();

            using (var file = File.OpenWrite(fileName))
                Writer.Write(file, shouldWriteSegmentSize, dataContext, synchronizationContext);

            Assert.AreEqual(0, dataContext.OutputData.Keys.Count());
            var expectedSize = itemsCount * (buffer.Length + (shouldWriteSegmentSize ? sizeof(int) : 0));
            using (var file = File.OpenRead(fileName))
                Assert.AreEqual(expectedSize, file.Length);
        }

        [TestCleanup]
        public void CleanUp()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.test");
            foreach (var file in files)
                File.Delete(file);
        }
    }
}
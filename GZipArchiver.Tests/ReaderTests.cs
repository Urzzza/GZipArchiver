using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipArchiver.Tests
{
    [TestClass]
    public class ReaderTests
    {
        [TestMethod]
        public void TestReader()
        {
            var fileSize = 1024 * 1024;
            var dataContext = new DataContext();
            var synchronizationContext = new SynchronizationContext();
            var buffer = FileGenerator.CreateOrReadCyclicFile(fileSize);
            var fileName = FileGenerator.GetInputFileName(fileSize);

            using (var file = File.OpenRead(fileName))
            {
                Reader.Read(file, false, buffer.Length, 1050, dataContext, synchronizationContext);
            }
            
            Assert.AreEqual(fileSize / buffer.Length, dataContext.InputData.Keys.Count());
        }
    }
}
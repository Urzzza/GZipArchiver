using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipTest.Tests
{
    [TestClass]
    public class GZipArchiverTests
    {
        private readonly Random rand = new Random();

        [DataTestMethod]
        [DataRow(1024 * 1024)] // 1 mb
        [DataRow(100 * 1024 * 1024)] // 100 mb
        [DataRow(1024 * 1024 * 1024)] // 1 Gb
        [DataRow(16L * 1024 * 1024 * 1024)] // 16 Gb
        public void TestFile(long fileSize)
        {
            var bufferSize = 4 * 1024;
            var inputFilename = $"{fileSize}.input";

            var buffer = new byte[bufferSize];

            if (!File.Exists(inputFilename))
            {
                rand.NextBytes(buffer);
                long alreadyWritten = 0;
                using (var file = File.Create(inputFilename))
                {
                    while (alreadyWritten < fileSize)
                    {
                        file.Write(buffer, 0, bufferSize);
                        alreadyWritten += bufferSize;
                    }
                }
            }
            else
            {
                using (var file = File.OpenRead(inputFilename))
                    file.Read(buffer, 0, bufferSize);
            }

            var compressedFileName = $"{fileSize}compressed.test";
            var decompressedFileName = $"{fileSize}decompressed.test";

            var sw = Stopwatch.StartNew();
            using (var input = File.OpenRead(inputFilename))
            using (var output = File.OpenWrite(compressedFileName))
            using (var compressor = new GZipArchiver(input, output, CompressionMode.Compress))
                compressor.Process();

            sw.Stop();
            Console.WriteLine($"Compressed in {sw.Elapsed.TotalSeconds} s");

            Assert.IsTrue(File.Exists(compressedFileName));

            sw.Restart();
            using (var input = File.OpenRead(compressedFileName))
            using (var output = File.OpenWrite(decompressedFileName))
            using (var compressor = new GZipArchiver(input, output, CompressionMode.Decompress))
                compressor.Process();

            sw.Stop();
            Console.WriteLine($"Decompressed in {sw.Elapsed.TotalSeconds} s");

            Assert.IsTrue(File.Exists(decompressedFileName));

            using (var file = File.OpenRead(decompressedFileName))
            {
                Assert.AreEqual(fileSize, file.Length);

                if (fileSize <= 100 * 1024 * 1024)  // takes way too much time for larger files
                {
                    var newData = new byte[bufferSize];
                    long alreadyChecked = 0;

                    while (alreadyChecked < file.Length)
                    {
                        file.Read(newData, 0, bufferSize);
                        CollectionAssert.AreEqual(buffer, newData);
                        alreadyChecked += bufferSize;
                    }
                }
            }
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

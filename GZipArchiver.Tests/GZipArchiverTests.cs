using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipArchiver.Tests
{
    [TestClass]
    public class GZipArchiverTests
    {
        [DataTestMethod]
        [DataRow(1024 * 1024)] // 1 mb
        [DataRow(100 * 1024 * 1024)] // 100 mb
        [DataRow(1024 * 1024 * 1024)] // 1 Gb
        [DataRow(16L * 1024 * 1024 * 1024)] // 16 Gb
        [DataRow(32L * 1024 * 1024 * 1024)] // 32 Gb
        public void TestFile(long fileSize)
        {
            var buffer = FileGenerator.CreateOrReadCyclicFile(fileSize);
            var inputFilename = FileGenerator.GetInputFileName(fileSize);
            var compressedFileName = $"{fileSize}compressed.test";
            var decompressedFileName = $"{fileSize}decompressed.test";
            var inputFileSize = 0L;

            var sw = Stopwatch.StartNew();
            using (var input = File.OpenRead(inputFilename))
            using (var output = File.OpenWrite(compressedFileName))
            using (var compressor = new GZipArchiver(input, output, CompressionMode.Compress))
            {
                inputFileSize = input.Length;
                compressor.Process();
            }

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
                Assert.AreEqual(inputFileSize, file.Length);

                var newData = new byte[buffer.Length];
                long alreadyChecked = 0;

                while (alreadyChecked < file.Length && alreadyChecked <= 100 * 1024 * 1024)
                {
                    file.Read(newData, 0, buffer.Length);
                    CollectionAssert.AreEqual(buffer, newData);
                    alreadyChecked += buffer.Length;
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

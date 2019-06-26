using System;
using System.IO;

namespace GZipArchiver.Tests
{
    public static class FileGenerator
    {
        private static Random rand = new Random();
        private static int bufferSize = 4 * 1024;

        public static byte[] CreateOrReadCyclicFile(long fileSize)
        {
            var inputFilename = GetInputFileName(fileSize);

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

            return buffer;
        }

        public static string GetInputFileName(long fileSize) => $"{fileSize}.input";
    }
}
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GZipArchiver.Tests
{
    public static class FileGenerator
    {
        public static byte[] CreateOrReadCyclicFile(long fileSize)
        {
            var inputFilename = GetInputFileName(fileSize);
            var book = Encoding.UTF8.GetBytes(Properties.Resources.book);

            var bufferSize = (int)Math.Min(book.Length, fileSize);

            if (!File.Exists(inputFilename))
            {
                long alreadyWritten = 0;
                using (var file = File.Create(inputFilename))
                {
                    while (alreadyWritten < fileSize)
                    {
                        file.Write(book, 0, bufferSize);
                        alreadyWritten += bufferSize;
                    }
                }
            }
            
            return fileSize > book.Length ? book : book.Take(bufferSize).ToArray();
        }

        public static string GetInputFileName(long fileSize) => $"{fileSize}.input";
    }
}
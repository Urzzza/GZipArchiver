using System;
using System.IO;
using System.IO.Compression;

namespace GZipArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine(GetUsage());
                return;
            }

            CompressionMode compressionMode;

            switch (args[0].ToLower())
            {
                case "compress":
                    compressionMode = CompressionMode.Compress;
                    break;
                case "decompress":
                    compressionMode = CompressionMode.Decompress;
                    break;
                default:
                    Console.WriteLine(GetUsage());
                    return;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Input file does not exist");
                return;
            }

            if (File.Exists(args[2]))
            {
                Console.WriteLine("Output file already exists, overwrite? (y/n)");
                var rewrite = Console.ReadKey();
                if (rewrite.KeyChar != 'y')
                {
                    Console.WriteLine("Please provide output file name");
                    return;
                }
            }

            try
            {
                using (var inputStream = new FileStream(args[1], FileMode.Open))
                using (var outStream = new FileStream(args[2], FileMode.Create))
                using (var fileProcessor = new GZipArchiver(inputStream, outStream, compressionMode))
                    fileProcessor.Process();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to process. Please restart the application. {e.Message}");
                Environment.Exit(1);
            }

        }

        private static string GetUsage()
        {
            return "Usage:" + Environment.NewLine + 
                   "GZipTest.exe compress/decompress [inputFile] [outputFile]";
        }
    }
}

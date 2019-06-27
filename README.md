# GZipArchiver
Command line tool using C# for block-by-block compressing and decompressing of files using class System.IO.Compression.GzipStream.

## What it does? 
Application effectively parallels and synchronize blocks processing in multicore environment and is able to process files, that are larger than available RAM size.

## Development limitations
Only basic classes and synchronization objects are used for multithreading (Thread, Manual/AutoResetEvent, Monitor, Semaphore, Mutex), it was not allowed to use async/await, ThreadPool, BackgroundWorker, TPL.

## Usage:
- compressing: GZipTest.exe compress [original file name] [archive file name]
- decompressing: GZipTest.exe decompress [archive file name] [decompressing file name]

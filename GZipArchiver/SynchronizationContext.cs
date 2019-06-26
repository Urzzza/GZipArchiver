using System.Threading;

namespace GZipArchiver
{
    public class SynchronizationContext
    {
        public ManualResetEvent ReaderEvent = new ManualResetEvent(true);
        public ManualResetEvent ZipperEvent = new ManualResetEvent(false);
        public ManualResetEvent WriterEvent = new ManualResetEvent(false);

        public bool FinishedReading = false;
    }
}
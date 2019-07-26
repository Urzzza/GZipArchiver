using System.Threading;

namespace GZipArchiver
{
    public class SynchronizationContext
    {
        public ManualResetEvent ZipperEvent = new ManualResetEvent(true);
        public ManualResetEvent WriterEvent = new ManualResetEvent(false);

        public bool FinishedReading = false;
    }
}
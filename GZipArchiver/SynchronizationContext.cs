using System.Threading;

namespace GZipArchiver
{
    public class SynchronizationContext
    {
        public ManualResetEvent WriterEvent = new ManualResetEvent(false);

        public bool FinishedReading = false;
    }
}
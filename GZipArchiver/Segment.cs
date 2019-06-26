namespace GZipTest
{
    public class Segment
    {
        public byte[] Data;
        public bool IsFinal;
        public int RetryCount = 0;

        public Segment(byte[] data, bool isFinal = false)
        {
            Data = data;
            IsFinal = isFinal;
        }
    }
}
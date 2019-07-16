namespace GZipArchiver
{
    public class DataContext
    {
        public SafeDictionary<int, Segment> OutputData { get; } = new SafeDictionary<int, Segment>();
    }
}
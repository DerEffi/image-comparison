namespace ImageComparison.Models
{
    public class ImageAnalysis
    {
        public FileInfo Image { get; set; }
        public ulong[] Hash { get; set; }
    }
}

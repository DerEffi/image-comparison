namespace ImageComparison.Models
{
    public class ImageAnalysis
    {
        public FileInfo Image { get; set; }
        public byte[] Hash { get; set; }
    }
}

namespace ImageComparison.Models
{
    public class ImageMatch
    {
        public ImageAnalysis Image1 { get; set; }
        public ImageAnalysis Image2 { get; set; }
        public short Similarity { get; set; }
    }
}

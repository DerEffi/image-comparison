namespace ImageComparison.Models
{
    public class Comparison
    {
        public FileInfo BaseImage { get; set; }
        public FileInfo CompareImage { get; set; }
        public float Result;
    }
}

using System.IO;

namespace ImageComparisonGUI.Models
{
    public class ImageComparison
    {
        public FileInfo BaseImage { get; set; }
        public FileInfo CompareImage { get; set; }
        public float Result;
    }
}

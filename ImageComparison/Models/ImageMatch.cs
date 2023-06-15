using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Models
{
    public class ImageMatch
    {
        public ImageAnalysis Image1 { get; set; }
        public ImageAnalysis Image2 { get; set; }
        public short Distance { get; set; }
    }
}

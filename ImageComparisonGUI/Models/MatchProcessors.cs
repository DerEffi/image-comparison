using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparisonGUI.Models
{
    public static class MatchProcessors
    {
        public static readonly List<string> Supported = new()
        {
            "Higher Resolution",
            "Bigger Filesize",
            "Newer File",
            "Right File",
            "None",
            "Lower Resolution",
            "Smaller Filesize",
            "Older File",
            "Left File"
        };
    }
}

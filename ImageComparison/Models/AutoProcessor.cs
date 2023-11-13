using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Models
{
    public class AutoProcessor
    {
        public string DisplayName { get; set; } = "None";
        /// <summary>
        /// Determines the file that should be deleted by the current processor
        /// </summary>
        /// <returns>negative value for first image, positive value for second image or 0 if can't be determined</returns>
        /// <exception cref="OperationCanceledException">Throws if no image should be deleted to continue with next image pair</exception>
        public Func<FileInfo, FileInfo, int> Process { get; set; } = (FileInfo a, FileInfo b) => throw new OperationCanceledException();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Models
{
    public class Log
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public DateTime Time{ get; set; } = DateTime.Now;
        public string Text { get; set; } = string.Empty;
    }
}

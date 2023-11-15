using ImageComparison.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Services
{
    public static class LogService
    {
        public static event EventHandler<LogEventArgs> OnLog = delegate { };

        public static List<Log> Logs { get; private set; } = new();

        public static void Log(Log log)
        {
            Logs.Add(log);
            OnLog.Invoke(null, new(){ Log = log});
        }

        public static void Log(string text, LogLevel logLevel = LogLevel.Info)
        {
            Log(new() { Text = text, LogLevel = logLevel });
        }
    }

    public class LogEventArgs: EventArgs
    {
        public Log Log { get; set; } = new();
    }
}

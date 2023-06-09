using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparisonGUI.Services
{
    public static class UrlService
    {
        private static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var tmp)) return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

        public static void OpenUrl(this string url)
        {
            if (!IsValidUrl(url)) throw new InvalidDataException("invalid url: " + url);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var proc = new Process { StartInfo = { UseShellExecute = true, FileName = url } };
                proc.Start();

                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("x-www-browser", url);
                return;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) throw new InvalidDataException("invalid url: " + url);
            Process.Start("open", url);
            return;
        }
    }
}

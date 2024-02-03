using ImageComparison.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Services
{
    /// <summary>
    /// Automatic processing of image matches to determine what image to move/delete
    /// </summary>
    public static class AutoProcessorService
    {
        /// <summary>
        /// Names of the implemented processors
        /// </summary>
        public static List<string> Supported { get => Processors.Select((p) => p.DisplayName).ToList(); }

        /// <summary>
        /// Processor Implementations
        /// </summary>
        public readonly static List<AutoProcessor> Processors = new()
        {
            new(){
                DisplayName = "Higher Resolution",
                Process = (FileInfo a, FileInfo b) => {
                    using (Stream aStream = File.OpenRead(a.FullName))
                    using (Stream bStream = File.OpenRead(b.FullName))
                    {
                        Image<Rgba32> aImg = Image.Load<Rgba32>(aStream);
                        Image<Rgba32> bImg = Image.Load<Rgba32>(bStream);
                        long aRes = aImg.Height * aImg.Width;
                        long bRes = bImg.Height * bImg.Width;
                        if(aRes > bRes)
                            return -1;
                        if(aRes < bRes)
                            return 1;
                        return 0;
                    }
                }
            },
            new(){
                DisplayName = "Bigger Filesize",
                Process = (FileInfo a, FileInfo b) =>
                {
                    if(a.Length > b.Length)
                        return -1;
                    if(a.Length < b.Length)
                        return 1;
                    return 0;
                }
            },
            new(){
                DisplayName = "Newer File",
                Process = (FileInfo a, FileInfo b) => {
                    int relation = DateTime.Compare(a.LastWriteTimeUtc, b.LastWriteTimeUtc);
                    if(relation > 0)
                        return -1;
                    if(relation < 0)
                        return 1;
                    return 0;
                }
            },
            new(){
                DisplayName = "Right File",
                Process = (FileInfo a, FileInfo b) => 1
            },
            new(){
                DisplayName = "None",
                Process = (FileInfo a, FileInfo b) => throw new OperationCanceledException()
            },
            new(){
                DisplayName = "Lower Resolution",
                Process = (FileInfo a, FileInfo b) => {
                    using (Stream aStream = File.OpenRead(a.FullName))
                    using (Stream bStream = File.OpenRead(b.FullName))
                    {
                        Image<Rgba32> aImg = Image.Load<Rgba32>(aStream);
                        Image<Rgba32> bImg = Image.Load<Rgba32>(bStream);
                        long aRes = aImg.Height * aImg.Width;
                        long bRes = bImg.Height * bImg.Width;
                        if(aRes < bRes)
                            return -1;
                        if(aRes > bRes)
                            return 1;
                        return 0;
                    }
                }
            },
            new(){
                DisplayName = "Smaller Filesize",
                Process = (FileInfo a, FileInfo b) => {
                    if(a.Length < b.Length)
                        return -1;
                    if(a.Length > b.Length)
                        return 1;
                    return 0;
                }
            },
            new(){
                DisplayName = "Older File",
                Process = (FileInfo a, FileInfo b) => {
                    int relation = DateTime.Compare(a.LastWriteTimeUtc, b.LastWriteTimeUtc);
                    if(relation < 0)
                        return -1;
                    if(relation > 0)
                        return 1;
                    return 0;
                }
            },
            new(){
                DisplayName = "Left File",
                Process = (FileInfo a, FileInfo b) => -1
            },
        };
    }
}

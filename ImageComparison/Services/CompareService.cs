using ImageComparison.Models;
using Emgu.CV.ImgHash;
using Emgu.CV.CvEnum;
using Emgu.CV;
using System.Runtime.InteropServices;

namespace ImageComparison.Services
{
    public class ImageComparerEventArgs
    {
        public int Current;
        public int Target;
    }

    public static class CompareService
    {
        public readonly static string[] SupportedFileTypes = { ".bmp", ".dib", ".jpg", ".jpeg", ".jpe", ".png", ".pbm", ".pgm", ".ppm", ".sr", ".ras", ".tiff", ".tif", ".exr", ".jp2" };
        
        public static event EventHandler<ImageComparerEventArgs> OnProgress = delegate {};

        public static void GetMatches(List<List<FileInfo>> searchLocations, SearchMode mode, CancellationToken? token)
        {
            List<ImageAnalysis> comparisons = new();
            int target = searchLocations.SelectMany(i => i).Count();
            int counter = 0;

            searchLocations.ForEach(location =>
            {
                location.ForEach(file =>
                {
                    if(token.HasValue && token.Value.IsCancellationRequested)
                        return;

                    try
                    {
                        comparisons.Add(new()
                        {
                            Image = file,
                            Hash = ComputeHash<PHash>(file.FullName)
                        });
                    }
                    catch (Exception) { }

                    if((++counter & 7) == 0) {
                        OnProgress.Invoke(null, new ImageComparerEventArgs()
                        {
                            Current = counter,
                            Target = target
                        });
                    }
                });
            });
        }

        private static byte[] ComputeHash<HashAlgorithm>(string file) where HashAlgorithm : ImgHashBase, new()
        {
            byte[] data;

            using (HashAlgorithm aHash = new())
            using (Mat result = new())
            using (Mat image = CvInvoke.Imread(file, ImreadModes.Color))
            {
                aHash.Compute(image, result);

                int hashLength = result.Width * result.Height;
                data = new byte[hashLength];
                Marshal.Copy(result.DataPointer, data, 0, hashLength);
            }

            return data;
        }
    }
}

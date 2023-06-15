using ImageComparison.Models;
using Emgu.CV.ImgHash;
using Emgu.CV.CvEnum;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Timers;
using System.Collections.Concurrent;
using CoenM.ImageHash;
using CoenM.ImageHash.HashAlgorithms;

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

        public static List<List<ImageAnalysis>> AnalyseImages(List<List<FileInfo>> searchLocations, CancellationToken token = new())
        {
            List<ConcurrentBag<ImageAnalysis>> analysed = new();

            using (System.Timers.Timer ProgressTimer = new())
            //using (PHash algorithm = new()) //EmguCV - when in use comment out ImageHash
            {
                DifferenceHash algorithm = new(); //ImageHash - when in use comment out EmguCV Hash
                int target = searchLocations.SelectMany(i => i).Count();

                //dont overload cpu with too many threads, leave one core free
                int threadCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1;
             
                //update caller with current progress with events
                ProgressTimer.Interval = 500;
                ProgressTimer.AutoReset = true;
                ProgressTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
                {
                    OnProgress.Invoke(null, new ImageComparerEventArgs()
                    {
                        Current = analysed.SelectMany(a => a).Count(),
                        Target = target
                    });
                };
                ProgressTimer.Start();

                //keep searchLocations separate for later comparisons depending on search mode
                searchLocations.ForEach(location =>
                {
                    ConcurrentBag<ImageAnalysis> locationAnalysis = new();
                    analysed.Add(locationAnalysis);

                    Parallel.ForEach(location, new(){ MaxDegreeOfParallelism = threadCount }, file =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        try
                        {
                            locationAnalysis.Add(new()
                            {
                                Image = file,
                                Hash = ComputeHash(file.FullName, algorithm)
                            });
                        }
                        catch (Exception) { }
                    });
                });

                ProgressTimer.Stop();

                OnProgress.Invoke(null, new ImageComparerEventArgs()
                {
                    Current = target,
                    Target = target
                });
            }

            return analysed.Select(a => a.ToList()).ToList();
        }

        public static List<ImageMatch> SearchForDuplicates(List<List<ImageAnalysis>> analysedLocations, SearchMode mode, CancellationToken token = new())
        {
            ConcurrentBag<ImageMatch> comparisons = new();

            //dont overload cpu with too many threads, leave one core free
            int threadCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1;

            analysedLocations.ForEach(location =>
            {
                Parallel.ForEach(location, new(){ MaxDegreeOfParallelism = threadCount },  (image, state, index) =>
                {
                    for(int i = Convert.ToInt32(index) + 1; i < location.Count; i++)
                    {

                        if (token.IsCancellationRequested)
                            return;

                        comparisons.Add(new(){
                            Image1 = image,
                            Image2 = location[i],
                            Distance = Convert.ToInt16(Math.Floor(CompareHash.Similarity(image.Hash, location[i].Hash)))
                        });
                    }
                });
            });

            return comparisons.ToList();
        }

        //Calculate Hash Values by EmguCV (OpenCV algorithms)
        private static byte[] ComputeHash(string file, ImgHashBase algorithm)
        {
            using (Mat result = new())
            using (Mat image = CvInvoke.Imread(file, ImreadModes.Color))
            {
                algorithm.Compute(image, result);

                int hashLength = result.Width * result.Height;
                byte[] data = new byte[hashLength];
                Marshal.Copy(result.DataPointer, data, 0, hashLength);

                return data;
            }
        }

        //Calculate Hash Values by ImageHash (Dr. Neal Krawetz algorithms)
        private static byte[] ComputeHash(string file, IImageHash algorithm)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return BitConverter.GetBytes(algorithm.Hash(stream));
            }
        }
    }
}

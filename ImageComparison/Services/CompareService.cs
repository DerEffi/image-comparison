using ImageComparison.Models;
using System.Timers;
using System.Collections.Concurrent;
using ImageComparison.Services.Hashs;
using System.IO;

namespace ImageComparison.Services
{
    public class ImageComparerEventArgs
    {
        public int Current;
        public int Target;
    }

    public static class CompareService
    {
        public readonly static string[] SupportedFileTypes = { ".bmp", ".dib", ".jpg", ".jpeg", ".jpe", ".png", ".pbm", ".pgm", ".ppm", ".sr", ".ras", ".tiff", ".tif", ".exr", ".jp2", ".ico" };
#if DEBUG
        //only use single thread for breakpoints
        public static readonly int threadCount = 8;
#else
        //dont overload cpu with too many threads, leave one core free
        public static readonly int threadCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1;
#endif
        public static event EventHandler<ImageComparerEventArgs> OnProgress = delegate {};

        public static List<List<ImageAnalysis>> AnalyseImages(List<List<FileInfo>> searchLocations, int hashDetail, HashAlgorithm hashAlgorithm, List<CacheItem>? cachedAnalysis, CancellationToken token = new())
        {
            LogService.Log($"Starting image analysation for {searchLocations.Count} location{(searchLocations.Count > 1 ? "s" : "")} with {hashAlgorithm}-{hashDetail}");

            cachedAnalysis ??= new List<CacheItem>();

            List<ConcurrentBag<ImageAnalysis>> analysed = new();

            using (System.Timers.Timer ProgressTimer = new())
            {
                IHashAlgorithm hash = hashAlgorithm switch
                {
                    HashAlgorithm.DHash => new DHash(hashDetail),
                    HashAlgorithm.DHashDouble => new DHashDouble(hashDetail),
                    _ => new PHash(hashDetail)
                };
                int target = searchLocations.SelectMany(i => i).Count();
             
                //update caller with current progress with events
                ProgressTimer.Interval = 500;
                ProgressTimer.AutoReset = true;
                ProgressTimer.Elapsed += (object? source, ElapsedEventArgs e) =>
                {
                    int current = analysed.SelectMany(a => a).Count();
                    OnProgress.Invoke(null, new ImageComparerEventArgs()
                    {
                        Current = current,
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
                        {
                            LogService.Log("Image analysation canceled by user", LogLevel.Warning);
                            return;
                        }

                        try
                        {
                            CacheItem? cachedImage = cachedAnalysis.FirstOrDefault(c => c.path == file.FullName);
                            locationAnalysis.Add(new()
                            {
                                Image = file,
                                Hash = cachedImage != null && cachedImage.hash != null && cachedImage.scantime > (ulong)(file.LastWriteTime - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds ? cachedImage.hashArray : hash.Hash(file.FullName)
                            });
                        }
                        catch (Exception e) {
                            LogService.Log($"Could not analyse '{file.FullName}'", LogLevel.Error);
                        }
                    });

                    LogService.Log($"Analysed location with {locationAnalysis.Count} images");
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

        public static List<ImageMatch> SearchForDuplicates(List<List<ImageAnalysis>> analysedLocations, int matchThreashold, SearchMode mode, List<NoMatch>? nomatches, CancellationToken token = new(), bool subsearch = false)
        {
            if(!subsearch)
                LogService.Log($"Comparing images from {analysedLocations.Count} locations in mode '{mode}'");

            nomatches ??= new();

            switch(mode)
            {
                case SearchMode.ListExclusive:
                    //calculate if many locations or many files within each location given
                    double filesPerLocation = ((double)analysedLocations.Count * analysedLocations.SelectMany(i => i).Count()) / analysedLocations.Count;

                    ConcurrentBag<ImageMatch> comparisons = new();
                    
                    //Run in parallel if more locations than file per location, else run files within locations in parallel
                    Parallel.ForEach(analysedLocations, new() { MaxDegreeOfParallelism = filesPerLocation <= analysedLocations.Count ? threadCount : 1 }, (images, state, currentLocation) =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            LogService.Log("Image comparison canceled by user", LogLevel.Warning);
                            return;
                        }

                        Parallel.ForEach(images, new() { MaxDegreeOfParallelism = filesPerLocation > analysedLocations.Count ? threadCount : 1 }, (image) =>
                        {
                            for(int location = (int)currentLocation + 1; location < analysedLocations.Count; location++)
                            {

                                analysedLocations[location].ForEach(comparer =>
                                {
                                    if (token.IsCancellationRequested)
                                    {
                                        LogService.Log("Image comparison canceled by user", LogLevel.Warning);
                                        return;
                                    }

                                    short similarity = CalculateSimilarity(image.Hash, comparer.Hash);
                                    if (similarity >= matchThreashold && !IsNoMatch(nomatches, image.Image.FullName, comparer.Image.FullName))
                                    {
                                        comparisons.Add(new()
                                        {
                                            Image1 = image,
                                            Image2 = comparer,
                                            Similarity = similarity
                                        });
                                    }
                                });
                            }
                        });
                    });

                    return subsearch ? comparisons.ToList() : SortMatches(comparisons);
                case SearchMode.ListInclusive:
                    return SortMatches(analysedLocations
                        .SelectMany(location => SearchForDuplicates(location, matchThreashold, nomatches, token))
                        .ToList());
                case SearchMode.Exclusive:
                    return SortMatches(SearchForDuplicates(
                        analysedLocations
                            .SelectMany(location =>
                                location
                                    .GroupBy(directory => directory.Image.DirectoryName)
                                    .Select(image => image.ToList())
                                    .ToList()
                            )
                        .ToList(),
                    matchThreashold,
                    SearchMode.ListExclusive,
                    nomatches,
                    token,
                    true));
                case SearchMode.Inclusive:
                    return SortMatches(analysedLocations
                        .SelectMany(images => {
                            return images
                                .GroupBy(image => image.Image.DirectoryName)
                                .SelectMany(directory => SearchForDuplicates(directory.ToList(), matchThreashold, nomatches, token));
                        })
                        .ToList());
                default:
                    return SortMatches(SearchForDuplicates(analysedLocations.SelectMany(images => images).ToList(), matchThreashold, nomatches, token));
            }
        }

        private static List<ImageMatch> SearchForDuplicates(List<ImageAnalysis> images, int matchThreashold, List<NoMatch> nomatches, CancellationToken token = new())
        {
            nomatches ??= new();

            ConcurrentBag<ImageMatch> comparisons = new();

            Parallel.ForEach(images, new() { MaxDegreeOfParallelism = threadCount }, (image, state, index) =>
            {
                for (int i = Convert.ToInt32(index) + 1; i < images.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        LogService.Log("Image comparison canceled by user", LogLevel.Warning);
                        return;
                    }

                    short similarity = CalculateSimilarity(image.Hash, images[i].Hash);
                    if (similarity >= matchThreashold && !IsNoMatch(nomatches, image.Image.FullName, images[i].Image.FullName))
                    {
                        comparisons.Add(new()
                        {
                            Image1 = image,
                            Image2 = images[i],
                            Similarity = similarity
                        });
                    }
                }
            });

            return comparisons.ToList();
        }

        private static short CalculateSimilarity(ulong[] hash1, ulong[] hash2)
        {
            return HashService.Similarity(hash1, hash2);
        }

        private static List<ImageMatch> SortMatches(ConcurrentBag<ImageMatch> matches)
        {
            return SortMatches(matches.ToList());
        }

        private static List<ImageMatch> SortMatches(List<ImageMatch> matches)
        {
            LogService.Log($"Sorting {matches.Count} found matches");

            // Sort images within a match
            matches.ForEach(m =>
            {
                if (string.Compare(m.Image1.Image.FullName, m.Image2.Image.FullName) < 0)
                    (m.Image1, m.Image2) = (m.Image2, m.Image1);
            });

            // Sort Matches
            matches.Sort((a,b) =>
            {
                int result = b.Similarity - a.Similarity;
                if (result == 0)
                    return string.Compare(a.Image1.Image.FullName, b.Image1.Image.FullName);

                return result;
            });

            return matches;
        }

        private static bool IsNoMatch(List<NoMatch> nomatches, string a, string b)
        {
            if(nomatches.Count == 0)
                return false;

            int order = string.Compare(a, b);
            if (order == 0)
                return true;
            else if (order < 0)
                (b, a) = (a, b);

            return nomatches.Any(n => n.a == a && n.b == b);
        }
    }
}

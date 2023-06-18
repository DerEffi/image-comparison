﻿using ImageComparison.Models;
using System.Timers;
using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection.Metadata.Ecma335;

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

        public static List<List<ImageAnalysis>> AnalyseImages(List<List<FileInfo>> searchLocations, int hashDetail, bool hashBothDirections, CancellationToken token = new())
        {
            List<ConcurrentBag<ImageAnalysis>> analysed = new();

            using (System.Timers.Timer ProgressTimer = new())
            {
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
                                Hash = CalculateHash(file.FullName, hashDetail, hashBothDirections)
                            });
                        }
                        catch (Exception e) { }
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

        public static List<ImageMatch> SearchForDuplicates(List<List<ImageAnalysis>> analysedLocations, int matchThreashold, SearchMode mode, CancellationToken token = new())
        {
            switch(mode)
            {
                case SearchMode.ListExclusive:
                    //calculate if many locations or many files within each location given
                    double filesPerLocation = ((double)analysedLocations.Count * analysedLocations.SelectMany(i => i).Count()) / analysedLocations.Count;

                    //dont overload cpu with too many threads, leave one core free
                    int threadCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1;

                    ConcurrentBag<ImageMatch> comparisons = new();
                    
                    //Run in parallel if more locations than file per location, else run files within locations in parallel
                    Parallel.ForEach(analysedLocations, new() { MaxDegreeOfParallelism = filesPerLocation <= analysedLocations.Count ? threadCount : 1 }, (images, state, currentLocation) =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        Parallel.ForEach(images, new() { MaxDegreeOfParallelism = filesPerLocation > analysedLocations.Count ? threadCount : 1 }, (image) =>
                        {
                            for(int location = (int)currentLocation + 1; location < analysedLocations.Count; location++)
                            {

                                analysedLocations[location].ForEach(comparer =>
                                {
                                    if (token.IsCancellationRequested)
                                        return;

                                    short similarity = CalculateSimilarity(image.Hash, comparer.Hash);
                                    if (similarity >= matchThreashold)
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

                    return SortMatches(comparisons);
                case SearchMode.ListInclusive:
                    return analysedLocations
                        .SelectMany(location => SearchForDuplicates(location, matchThreashold, token))
                        .ToList();
                case SearchMode.Exclusive:
                    return SearchForDuplicates(
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
                    token);
                case SearchMode.Inclusive:
                    return analysedLocations
                        .SelectMany(images => {
                            return images
                                .GroupBy(image => image.Image.DirectoryName)
                                .SelectMany(directory => SearchForDuplicates(directory.ToList(), matchThreashold, token));
                        })
                        .ToList();
                default:
                    return SearchForDuplicates(analysedLocations.SelectMany(images => images).ToList(), matchThreashold, token);
            }
        }

        public static List<ImageMatch> SearchForDuplicates(List<ImageAnalysis> images, int matchThreashold, CancellationToken token = new())
        {
            ConcurrentBag<ImageMatch> comparisons = new();

            //dont overload cpu with too many threads, leave one core free
            int threadCount = Environment.ProcessorCount > 1 ? Environment.ProcessorCount - 1 : 1;

            Parallel.ForEach(images, new() { MaxDegreeOfParallelism = threadCount }, (image, state, index) =>
            {
                for (int i = Convert.ToInt32(index) + 1; i < images.Count; i++)
                {

                    if (token.IsCancellationRequested)
                        return;

                    short similarity = CalculateSimilarity(image.Hash, images[i].Hash);
                    if (similarity >= matchThreashold)
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

            return SortMatches(comparisons);
        }

        //Calculate Hash Values by ImageHash (Dr. Neal Krawetz algorithms)
        private static ulong[] CalculateHash(string file, int detail, bool bothDirections)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return HashService.DHash(Image.Load<Rgba32>(stream), detail, bothDirections);
            }
        }

        private static short CalculateSimilarity(ulong[] hash1, ulong[] hash2)
        {
            return HashService.Similarity(hash1, hash2);
        }

        private static List<ImageMatch> SortMatches(ConcurrentBag<ImageMatch> comparisons)
        {
            List<ImageMatch> matches = comparisons.ToList();
            matches.Sort((a,b) =>
            {
                int result = b.Similarity - a.Similarity;
                if (result == 0)
                    return string.Compare(a.Image1.Image.FullName, b.Image1.Image.FullName);

                return result;
            });

            return matches;
        }
    }
}

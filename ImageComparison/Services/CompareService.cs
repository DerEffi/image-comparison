﻿using ImageComparison.Models;
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

                                    short similarity = Convert.ToInt16(Math.Floor(CompareHash.Similarity(image.Hash, comparer.Hash)));
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

                    return comparisons.OrderByDescending(m => m.Similarity).ToList();
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

                    short similarity = Convert.ToInt16(Math.Floor(CompareHash.Similarity(image.Hash, images[i].Hash)));
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

            return comparisons.OrderByDescending(m => m.Similarity).ToList();
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

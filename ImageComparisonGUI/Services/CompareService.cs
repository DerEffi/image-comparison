using ImageComparisonGUI.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImageComparisonGUI.Services
{
    public class ImageComparerEventArgs
    {
        public int Current;
        public int Target;
    }

    public static class CompareService
    {
        public static event EventHandler<ImageComparerEventArgs> OnProgress = delegate {};

        public static List<ImageComparison> GetMatches(List<List<FileInfo>> folders, SearchMode mode)
        {
            List<ImageComparison> comparisons = new();

            if (mode == SearchMode.ListInclusive || mode == SearchMode.Inclusive)
            {
                folders.ForEach(folder =>
                {
                    comparisons.AddRange(GetMatches(new() { folder }, mode == SearchMode.Inclusive ? mode : SearchMode.All));
                });
            } else {
                
            }

            return comparisons;
        }
    }
}

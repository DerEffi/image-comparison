using ImageComparison.Models;

namespace ImageComparison.Services
{
    public class ImageComparerEventArgs
    {
        public int Current;
        public int Target;
    }

    public static class CompareService
    {
        public static event EventHandler<ImageComparerEventArgs> OnProgress = delegate {};

        public static List<Comparison> GetMatches(List<List<FileInfo>> folders, SearchMode mode)
        {
            List<Comparison> comparisons = new();

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

using ImageComparison.Models;
using System;
using System.IO;

namespace ImageComparisonGUI.Services
{
    public static class ConfigService
    {
        public static event EventHandler OnUpdate = delegate { };

        //Processing settings
        public static int MatchThreashold { get; private set; } = 8000;
        public static int HashDetail { get; private set; } = 20;

        //Location settings
        public static string[] SearchLocations { get; private set; } = new string[] { };
        public static SearchMode SearchMode { get; private set; } = SearchMode.All;
        public static bool SearchSubdirectories { get; private set; } = true;

        //Deletion settings
        public static DeleteAction DeleteAction { get; private set; } = DeleteAction.RecycleBin;
        public static string DeleteTarget { get; private set; } = "Duplicates\\";
        public static bool RelativeDeleteTarget { get; private set; }


        public static void UpdateDeleteAction(DeleteAction action, string? target, bool? relativeTarget)
        {
            if(action == DeleteAction.Move)
            {
                if(target == null)
                    throw new ArgumentNullException(nameof(target));

                bool targetIsRelative = relativeTarget.HasValue ? relativeTarget.Value : RelativeDeleteTarget;
                if (targetIsRelative)
                {
                    while (target.StartsWith("."))
                        target = target.Substring(1);

                    while (target.StartsWith("/") || target.StartsWith("\\"))
                        target = target.Substring(1);
                }
                else if (!Path.IsPathRooted(target))
                    throw new DirectoryNotFoundException();

                DeleteTarget = target;
                RelativeDeleteTarget = targetIsRelative;
            }

            DeleteAction = action;

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        public static void UpdateSearchLocations(SearchMode mode, string[] locations, bool recursive) 
        {
            SearchMode = mode;
            SearchLocations = locations;
            SearchSubdirectories = recursive;

            OnUpdate.Invoke(null, EventArgs.Empty);
        }
    }
}

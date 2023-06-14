using ImageComparison.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImageComparisonGUI.Services
{
    public static class ConfigService
    {
        public static event EventHandler OnUpdate = delegate { };

        //Location settings
        private static string[] searchLocations = new string[] { "D:\\Bilder\\Allgemein" };
        public static string[] SearchLocations { get => searchLocations; }

        private static SearchMode searchMode = SearchMode.All;
        public static SearchMode SearchMode { get => searchMode; }

        private static bool searchSubdirectories = true;
        public static bool SearchSubdirectories { get => searchSubdirectories; }

        //Deletion settings
        private static DeleteAction deleteAction = DeleteAction.RecycleBin;
        public static DeleteAction DeleteAction { get => deleteAction; }

        private static string deleteTarget = "Duplicates\\";
        public static string DeleteTarget { get => deleteTarget; }
        
        private static bool relativeDeleteTarget = true;
        public static bool RelativeDeleteTarget { get => relativeDeleteTarget; }


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

                deleteTarget = target;
                relativeDeleteTarget = targetIsRelative;
            }

            deleteAction = action;

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        public static void UpdateSearchLocations(SearchMode mode, string[] locations, bool recursive) 
        {
            searchMode = mode;
            searchLocations = locations;
            searchSubdirectories = recursive;

            OnUpdate.Invoke(null, EventArgs.Empty);
        }
    }
}

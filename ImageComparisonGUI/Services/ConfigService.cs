using ImageComparisonGUI.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImageComparisonGUI.Services
{
    public static class ConfigService
    {
        private static string[] supportedFileTypes = { ".jpg", ".jpeg", ".png" };
        public static string[] SupportedFileTypes { get => supportedFileTypes; }

        private static List<string> searchLocations = new();
        public static List<string> SearchLocations { get => searchLocations; }

        private static DeleteAction deleteAction = DeleteAction.RecycleBin;
        public static DeleteAction DeleteAction { get => deleteAction; }

        private static string deleteTarget = "Duplicates\\";
        public static string DeleteTarget { get => deleteTarget; }
        
        private static bool relativeDeleteTarget = true;
        public static bool RelativeDeleteTarget { get => relativeDeleteTarget; }

        public static void UpdateDeleteAction(DeleteAction deleteAction, string? target, bool? relativeTarget)
        {
            if(deleteAction == DeleteAction.Move)
            {
                if(target == null)
                    throw new ArgumentNullException(nameof(target));

                bool targetIsRelative = relativeTarget.HasValue ? relativeTarget.Value : ConfigService.RelativeDeleteTarget;
                if (targetIsRelative)
                {
                    while (target.StartsWith("."))
                        target = target.Substring(1);

                    while (target.StartsWith("/") || target.StartsWith("\\"))
                        target = target.Substring(1);
                }
                else if (!Path.IsPathRooted(target))
                    throw new DirectoryNotFoundException();

                ConfigService.deleteTarget = target;
                ConfigService.relativeDeleteTarget = targetIsRelative;
            }

            ConfigService.deleteAction = deleteAction;
        }
    }
}

using ImageComparison.Models;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Immutable;
using System.IO;

namespace ImageComparison.Services
{
    public static class FileService
    {
        /// <summary>
        /// Directory for saving meta data (profiles, settings, cache)
        /// </summary>
        public static string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DerEffi", "ImageComparison");
        /// <summary>
        /// Filename for the cache database
        /// </summary>
        public static string CacheFile = "Cache.db";

        /// <summary>
        /// Process file (Move, Bin or Delete)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="deleteAction"></param>
        /// <param name="target"></param>
        /// <param name="relativeTarget"></param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public static void DeleteFile(string path, DeleteAction deleteAction = DeleteAction.Delete, string target = "Duplicates\\", bool relativeTarget = true)
        {
            if (path == null || !File.Exists(path))
            {
                LogService.Log($"Could not find file for deleting: '{path}'", LogLevel.Warning);
                throw new FileNotFoundException();
            }

            switch(deleteAction)
            {
                case DeleteAction.Delete:
                    File.Delete(path);
                    LogService.Log($"Deleted file: '{path}'");
                    break;
                case DeleteAction.RecycleBin:
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    LogService.Log($"Moved file to Recycle Bin: '{path}'");
                    break;
                case DeleteAction.Move:

                    if (string.IsNullOrEmpty(Path.GetDirectoryName(path)))
                    {
                        LogService.Log($"Could not find parent folder of '{path}'", LogLevel.Warning);
                        throw new DirectoryNotFoundException();
                    }

                    string targetPath = relativeTarget ? Path.Combine(Path.GetDirectoryName(path), target) : target;

                    if (File.Exists(targetPath))
                    {
                        LogService.Log($"Can not move file; target already exists: '{targetPath}'", LogLevel.Warning);
                        throw new IOException();
                    }

                    if (!Directory.Exists(targetPath))
                    {
                        LogService.Log($"Creating folder '{targetPath}' for moving files");
                        Directory.CreateDirectory(targetPath);
                    }

                    // Append number to the filename to prevent overriding already existing images until free filename is found
                    string targetFile = Path.Combine(targetPath + Path.GetFileName(path));
                    int counter = 0;
                    while (File.Exists(targetFile))
                    {
                        targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(path) + "-" + ++counter + Path.GetExtension(path));
                    }

                    File.Move(path, targetFile);

                    LogService.Log($"Moved '{path}' to '{targetFile}'");

                    break;
            }
        }

        /// <summary>
        /// Search all given locations for images to analyse
        /// </summary>
        /// <param name="searchLocations"></param>
        /// <param name="searchSubdirectories"></param>
        /// <returns></returns>
        public static List<List<FileInfo>> SearchProcessableFiles(string[] searchLocations, bool searchSubdirectories)
        {
            LogService.Log($"Searching for images in {searchLocations.Length} location{(searchLocations.Length > 1 ? "s" : "")}{(searchSubdirectories ? " recursively" : "")}");
            List<List<FileInfo>> images = GetProcessableFiles(searchLocations, searchSubdirectories);
            LogService.Log($"Found {images.Sum(i => i.Count)} images");

            return images;
        }

        // Recursion function for file search
        private static List<List<FileInfo>> GetProcessableFiles(string[] searchLocations, bool searchSubdirectories)
        {
            List<List<FileInfo>> directories = new();

            foreach(string location in searchLocations)
            {
                List<FileInfo> directory = new();

                if (string.IsNullOrEmpty(location) || !Directory.Exists(location))
                    continue;
                
                try
                {
                    List<FileInfo> current = Directory
                        .GetFiles(location, $"*.*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(path => CompareService.SupportedFileTypes.Any(ext => path.ToLower().EndsWith(ext)))
                        .Select(path => new FileInfo(path))
                        .ToList();

                    if(current.Count != 0)
                        directory.AddRange(current);

                    if(searchSubdirectories)
                        directory.AddRange(GetProcessableFiles(Directory.GetDirectories(location), true).SelectMany(i => i));
                } catch {
                    LogService.Log($"Error searching location '{location}'", LogLevel.Error);
                }

                if (directory.Count != 0)
                    directories.Add(directory);
            }

            return directories;
        }
    }
}

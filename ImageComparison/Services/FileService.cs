using ImageComparison.Models;
using Microsoft.VisualBasic.FileIO;

namespace ImageComparison.Services
{
    public static class FileService
    {
        public static void DeleteFile(string path, DeleteAction deleteAction = DeleteAction.Delete, string target = "Duplicates\\", bool relativeTarget = true)
        {
            if (path == null || !File.Exists(path))
                throw new FileNotFoundException();

            switch(deleteAction)
            {
                case DeleteAction.Delete:
                    File.Delete(path);
                    break;
                case DeleteAction.RecycleBin:
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    break;
                case DeleteAction.Move:

                    if (string.IsNullOrEmpty(Path.GetDirectoryName(path)))
                        throw new DirectoryNotFoundException();

                    string targetPath = relativeTarget ? Path.Combine(Path.GetDirectoryName(path), target) : target;

                    if (File.Exists(targetPath))
                        throw new IOException();

                    if(!Directory.Exists(targetPath))
                        Directory.CreateDirectory(targetPath);

                    string targetFile = Path.Combine(targetPath + Path.GetFileName(path));
                    int counter = 0;
                    while (File.Exists(targetFile))
                    {
                        targetFile = Path.Combine(targetPath, Path.GetFileNameWithoutExtension(path) + "-" + ++counter + Path.GetExtension(path));
                    }

                    File.Move(path, targetFile);
                    
                    break;
            }
        }

        public static List<List<FileInfo>> GetProcessableFiles(string[] searchLocations, bool searchSubdirectories)
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
                } catch { }

                if (directory.Count != 0)
                    directories.Add(directory);
            }

            return directories;
        }
    }
}

using ImageComparisonGUI.Models;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageComparisonGUI.Services
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

        public static string GetReadableFilesize(string path)
        {
            long size = (new FileInfo(path)).Length;
            if(size < 1000)
            {
                return $"{size} B";
            } else if (size < 1000000)
            {
                return $"{size >> 10} KB";
            } else if (size < 10000000)
            {
                return $"{decimal.Divide(size, 1000000):0.00} MB";
            } else if (size < 100000000)
            {
                return $"{(decimal.Divide(size, 1000000)):0.0} MB";
            } else
            {
                return $"{size >> 20} MB";
            }
        }

        public static List<SearchFolder> GetProcessableFiles(string[] searchFolders)
        {
            List<SearchFolder> result = new();

            foreach(string folder in searchFolders)
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    continue;
                
                try
                {
                    SearchFolder current = new()
                    {
                        Path = folder,
                        Files = Directory
                            .GetFiles(folder, $"*.*", System.IO.SearchOption.TopDirectoryOnly)
                            .Where(name => ConfigService.SupportedFileTypes.Any(ext => name.ToLower().EndsWith(ext)))
                            .ToList(),
                        Folders = GetProcessableFiles(Directory.GetDirectories(folder))
                    };

                    if(current.Files.Count != 0 || current.Folders.Count != 0)
                        result.Add(current);
                } catch { }
            }

            return result;
        }
    }
}

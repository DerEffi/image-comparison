using Dapper;
using ImageComparison.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Services
{
    public static class CacheService
    {
        private static SqliteConnection connection;

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(FileService.DataDirectory);
                connection = new SqliteConnection($"Data Source={Path.Combine(FileService.DataDirectory, "Cache.db")}");
                connection.Open();
                connection.Execute("CREATE TABLE IF NOT EXISTS file (id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, path TEXT NOT NULL COLLATE NOCASE, scantime INTEGER NOT NULL, size INTEGER, hashtype TEXT, hash BLOB, UNIQUE(path, hashtype))");
                connection.Execute("CREATE TABLE IF NOT EXISTS nomatch (a INTEGER REFERENCES file(id) ON DELETE CASCADE ON UPDATE NO ACTION DEFERRABLE INITIALLY DEFERRED, b INTEGER REFERENCES file(id) ON DELETE CASCADE ON UPDATE NO ACTION DEFERRABLE INITIALLY DEFERRED, UNIQUE(a, b))");
                connection.Execute("CREATE INDEX IF NOT EXISTS idxf_ht ON file(hashtype)");
                connection.Close();
            } catch { }
        }

        public static List<CacheItem> GetImages(string hashtype)
        {
            List<CacheItem> images = new();
            
            try
            {
                connection.Open();

                images = connection.Query<CacheItem>("SELECT path, scantime, size, hash FROM file WHERE hashtype = @Hashtype", new { Hashtype = hashtype }).ToList();
            } catch { }

            connection.Close();

            return images;
        }

        public static void UpdateImages(List<ImageAnalysis> images, string hashtype, ulong scantime)
        {
            try
            {
                connection.Open();
                using (SqliteTransaction transaction = connection.BeginTransaction())
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO file (path, scantime, size, hashtype, hash) VALUES (@Path, @Scantime, @Size, @Hashtype, @Hash) ON CONFLICT(path, hashtype) DO UPDATE SET scantime=@Scantime, size=@Size, hash=@Hash";
                    command.Parameters.AddWithValue("@Path", "C:\\");
                    command.Parameters.AddWithValue("@Scantime", 0);
                    command.Parameters.AddWithValue("@Size", 0);
                    command.Parameters.AddWithValue("@Hashtype", "");
                    command.Parameters.AddWithValue("@Hash", Array.Empty<byte>());

                    images.ForEach(image =>
                    {
                        command.Parameters["@Path"].Value = image.Image.FullName;
                        command.Parameters["@Scantime"].Value = scantime;
                        command.Parameters["@Size"].Value = image.Image.Length;
                        command.Parameters["@Hashtype"].Value = hashtype;
                        command.Parameters["@Hash"].Value = image.HashBlob;
                        command.ExecuteNonQuery();
                    });

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
            }

            connection.Close();
        }
    }
}

﻿using Dapper;
using ImageComparison.Models;
using Microsoft.Data.Sqlite;

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
                connection.Execute("CREATE TABLE IF NOT EXISTS file (path TEXT NOT NULL COLLATE NOCASE, scantime INTEGER NOT NULL, size INTEGER, hashtype TEXT, hash BLOB, UNIQUE(path, hashtype))");
                connection.Execute("CREATE TABLE IF NOT EXISTS nomatch (a TEXT NOT NULL COLLATE NOCASE, b TEXT NOT NULL COLLATE NOCASE, UNIQUE(a, b))");
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
            catch { }

            connection.Close();
        }

        public static void AddNoMatch(string a, string b)
        {
            try
            {
                int order = string.Compare(a, b);
                if (order == 0)
                    return;
                else if (order < 0)
                    (b, a) = (a, b);

                connection.Open();
                connection.Execute("INSERT INTO nomatch (a, b) VALUES (@a, @b) ON CONFLICT(a, b) DO NOTHING", new { a, b });
            }
            catch { }

            connection.Close();
        }

        public static void AddNoMatches(List<ImageMatch> nomatches)
        {
            try
            {
                connection.Open();
                using (SqliteTransaction transaction = connection.BeginTransaction())
                using (SqliteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO nomatch (a, b) VALUES (@a, @b) ON CONFLICT(a, b) DO NOTHING";
                    command.Parameters.AddWithValue("@a", "");
                    command.Parameters.AddWithValue("@b", "");

                    nomatches.ForEach(nomatch =>
                    {
                        string a, b;
                        int order = string.Compare(nomatch.Image1.Image.FullName, nomatch.Image2.Image.FullName);
                        if (order == 0)
                            return;
                        else if (order < 0)
                            (b, a) = (nomatch.Image1.Image.FullName, nomatch.Image2.Image.FullName);
                        else
                            (a, b) = (nomatch.Image1.Image.FullName, nomatch.Image2.Image.FullName);

                        command.Parameters["@a"].Value = a;
                        command.Parameters["@b"].Value = b;
                        command.ExecuteNonQuery();
                    });

                    transaction.Commit();
                }
            }
            catch { }

            connection.Close();
        }

        public static List<NoMatch> GetNoMatches()
        {
            List<NoMatch> nomatches = new();

            try
            {
                connection.Open();

                nomatches = connection.Query<NoMatch>("SELECT * FROM nomatch").ToList();
            }
            catch { }

            connection.Close();

            return nomatches;
        }

        public static void ClearImageCache()
        {
            try
            {
                connection.Open();
                connection.Execute("DELETE FROM file");
            } catch { }

            connection.Close();
        }

        public static void ClearNoMatchCache()
        {
            try
            {
                connection.Open();
                connection.Execute("DELETE FROM nomatch");
            } catch { }

            connection.Close();
        }
    }
}

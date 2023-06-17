using ImageComparison.Models;
using ImageComparisonGUI.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageComparisonGUI.Services
{
    public static class ConfigService
    {
        public static event EventHandler OnUpdate = delegate { };

        private static readonly string settingsFileName = "settings.json";
        public static readonly string ProfilesDirectory = "Profiles";
        public static readonly string DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DerEffi", "ImageComparison");
        private static Settings settings = new();
        private static List<Profile> profiles = new List<Profile>();
        public static List<string> Profiles { get => profiles.Select(p => p.Name).ToList(); }

        public static bool IsLocked { get; private set; } = false;

        //Processing settings
        public static int MatchThreashold { get => settings.MatchThreashold; }
        public static int HashDetail { get => settings.HashDetail; }

        //Location settings
        public static string[] SearchLocations { get => settings.SearchLocations; }
        public static SearchMode SearchMode { get => settings.SearchMode; }
        public static bool SearchSubdirectories { get => settings.SearchSubdirectories; }

        //Deletion settings
        public static DeleteAction DeleteAction { get => settings.DeleteAction; }
        public static string DeleteTarget { get => settings.DeleteTarget; }
        public static bool RelativeDeleteTarget { get => settings.RelativeDeleteTarget; }

        public static void Init()
        {
            LoadConfig();
            LoadProfiles();
        }

        public static void Lock()
        {
            IsLocked = true;
            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        public static void Unlock()
        {
            IsLocked = false;
            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        public static void UpdateDeleteAction(DeleteAction action, string? target, bool? relativeTarget)
        {
            if (!IsLocked)
            {
                if (action == DeleteAction.Move)
                {
                    if (target == null)
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

                    settings.DeleteTarget = target;
                    settings.RelativeDeleteTarget = targetIsRelative;
                }

                settings.DeleteAction = action;

                SaveConfig();
            }

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        public static void UpdateSearchLocations(SearchMode mode, string[] locations, bool recursive)
        {
            if(!IsLocked) {
                settings.SearchMode = mode;
                settings.SearchLocations = locations;
                settings.SearchSubdirectories = recursive;

                SaveConfig();
            }

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        private static void SaveConfig()
        {
            try
            {
                SaveConfig(Path.Combine(DataDirectory, settingsFileName));
            }
            catch (Exception) { }
        }

        private static void SaveConfig(string filePath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (dir != null) {
                    Directory.CreateDirectory(dir);
                    using FileStream stream = File.Create(filePath);
                    byte[] content = new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(settings, Formatting.Indented));
                    stream.Write(content, 0, content.Length);
                    stream.Close();
                }
            } catch(Exception) { }
        }

        private static void LoadConfig()
        {
            try
            {
                string settingsFile = Path.Combine(DataDirectory, settingsFileName);
                Settings? loadedSettings = LoadProfileFromFile(settingsFile);
                if (loadedSettings != null)
                {
                    settings = loadedSettings;
                    OnUpdate.Invoke(null, EventArgs.Empty);
                }

            } catch(Exception) { }
        }

        private static Settings? LoadProfileFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                Settings? loadedSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(filePath));
                if (loadedSettings != null && loadedSettings.GetType() == typeof(Settings))
                {
                    return loadedSettings;
                }
            }
            return null;
        }

        private static void LoadProfiles()
        {
            try
            {
                string profilesDir = Path.Combine(DataDirectory, ProfilesDirectory);
                Directory.CreateDirectory(profilesDir);
                profiles = Directory
                    .GetFiles(profilesDir, "*.json")
                    .Select(file => new Profile() { Name = Path.GetFileNameWithoutExtension(file), Settings = LoadProfileFromFile(file) })
                    .Where(profile => profile.Settings != null)
                    .ToList();

                OnUpdate.Invoke(null, EventArgs.Empty);
            } catch(Exception) { }
        }

        public static void LoadProfile(string name)
        {
            try
            {
                Settings? profile = profiles.Where(p => p.Name == name).Select(p => p.Settings).FirstOrDefault();
                if(profile != null)
                {
                    settings = profile.Clone();
                }

                OnUpdate.Invoke(null, EventArgs.Empty);
            }
            catch(Exception) { }
        }

        public static void RemoveProfile(string name)
        {
            try
            {
                string file = Path.Combine(DataDirectory, ProfilesDirectory, name + ".json");
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                LoadProfiles();
            }
            catch (Exception) { }
        }

        public static void SaveConfigAsProfile(string name)
        {
            try
            {
                name = new string(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException();

                string profilesDir = Path.Combine(DataDirectory, ProfilesDirectory);
                Directory.CreateDirectory(profilesDir);
                using FileStream stream = File.Create(Path.Combine(profilesDir, name + ".json"));
                byte[] content = new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(settings, Formatting.Indented));
                stream.Write(content, 0, content.Length);
                stream.Close();

                LoadProfiles();

            } catch(Exception) { }
        }
    }
}

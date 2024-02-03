using ImageComparison.Models;
using ImageComparison.Services;
using ImageComparisonGUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageComparisonGUI.Services
{
    public static class ConfigService
    {
        /// <summary>
        /// Notify on changes in config
        /// </summary>
        public static event EventHandler OnUpdate = delegate { };

        private static readonly string settingsFileName = "settings.json";
        public static readonly string ProfilesDirectory = "Profiles";
        private static Settings settings = new();
        private static List<Profile> profiles = new();
        public static List<string> Profiles { get => profiles.Select(p => p.Name).ToList(); }

        public static bool IsLocked { get; private set; } = false;

        //Auto Processing settings
        public static List<string> AutoProcessors { get => settings.AutoProcessors; }
        public static int AutoProcessorThreashold { get => settings.AutoProcessorThreashold; }

        //Cache settings
        public static bool FillNoMatchCache { get; private set; } = false;
        public static bool CacheNoMatch { get => settings.CacheNoMatch; }
        public static bool CacheImages { get => settings.CacheImages; }

        //Processing settings
        public static int MatchThreashold { get => settings.MatchThreashold; }
        public static int HashDetail { get => settings.HashDetail; }
        public static HashAlgorithm HashAlgorithm { get => settings.HashAlgorithm; }

        //Location settings
        public static string[] SearchLocations { get => settings.SearchLocations; }
        public static SearchMode SearchMode { get => settings.SearchMode; }
        public static bool SearchSubdirectories { get => settings.SearchSubdirectories; }

        //Deletion settings
        public static DeleteAction DeleteAction { get => settings.DeleteAction; }
        public static string DeleteTarget { get => settings.DeleteTarget; }
        public static bool RelativeDeleteTarget { get => settings.RelativeDeleteTarget; }

        public static List<Hotkey> Hotkeys { get { settings.DistinguishHotkeys(); return settings.Hotkeys; } }

        /// <summary>
        /// Load data from config folder (Once on application start)
        /// </summary>
        public static void Init()
        {
            LoadConfig();
            LoadProfiles();
        }

        /// <summary>
        /// Don't allow changes
        /// </summary>
        public static void Lock()
        {
            IsLocked = true;
            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Allow changes
        /// </summary>
        public static void Unlock()
        {
            IsLocked = false;
            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Update processing actions for found images in found matches
        /// </summary>
        /// <param name="action"></param>
        /// <param name="target"></param>
        /// <param name="relativeTarget"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
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

        /// <summary>
        /// Change locations to search for images
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="locations"></param>
        /// <param name="recursive"></param>
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

        /// <summary>
        /// Change parameters for image analysis
        /// </summary>
        /// <param name="matchThreashold"></param>
        /// <param name="hashDetail"></param>
        /// <param name="hashAlgorithm"></param>
        public static void UpdateAdjustables(int matchThreashold, int hashDetail, HashAlgorithm hashAlgorithm)
        {
            if (!IsLocked)
            {
                settings.MatchThreashold = matchThreashold;
                settings.HashAlgorithm = hashAlgorithm;
                settings.HashDetail = hashDetail;

                SaveConfig();
            }

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Change configured actions on keyboard input
        /// </summary>
        /// <param name="hotkeys"></param>
        public static void UpdateHotkeys(List<Hotkey> hotkeys)
        {
            settings.Hotkeys = hotkeys.ToList();
            settings.DistinguishHotkeys();

            SaveConfig();

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Change order of properties to determine how to auto process a found match
        /// </summary>
        /// <param name="processors"></param>
        /// <param name="threashold"></param>
        public static void UpdateAutoProcessors(List<string> processors, int threashold)
        {
            if(!IsLocked)
            {
                settings.AutoProcessorThreashold = threashold;
                settings.AutoProcessors = processors;
                settings.EnsureAutoProcessors();

                SaveConfig();
            }

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Change caching settings for image analysis and comparison
        /// </summary>
        /// <param name="cacheImages"></param>
        /// <param name="cacheNoMatch"></param>
        /// <param name="fillNoMatchCache"></param>
        public static void UpdateCache(bool cacheImages, bool cacheNoMatch, bool fillNoMatchCache)
        {
            settings.CacheImages = cacheImages;
            settings.CacheNoMatch = cacheNoMatch;
            FillNoMatchCache = fillNoMatchCache;

            SaveConfig();

            OnUpdate.Invoke(null, EventArgs.Empty);
        }

        private static void SaveConfig()
        {
            SaveConfig(Path.Combine(FileService.DataDirectory, settingsFileName));
        }

        private static void SaveConfig(string filePath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (dir != null) {
                    Directory.CreateDirectory(dir);
                    using FileStream stream = File.Create(filePath);
                    byte[] content = settings.GetContent();
                    stream.Write(content, 0, content.Length);
                    stream.Close();
                }
                LogService.Log($"Updated settings in '{filePath}'");
            } catch(Exception) {
                LogService.Log($"Error writing settings to '{filePath}'", LogLevel.Error);
            }
        }

        private static void LoadConfig()
        {
            try
            {
                string settingsFile = Path.Combine(FileService.DataDirectory, settingsFileName);
                Settings? loadedSettings = LoadProfileFromFile(settingsFile);
                if (loadedSettings != null)
                {
                    settings = loadedSettings;
                    OnUpdate.Invoke(null, EventArgs.Empty);
                }

                LogService.Log($"Settings loaded from '{settingsFile}'", LogLevel.Info);
            } catch(Exception) {
                LogService.Log($"Error loading settings", LogLevel.Error);
            }
        }

        private static Settings? LoadProfileFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                Settings? loadedSettings = Settings.Parse(File.ReadAllText(filePath));
                if (loadedSettings != null && loadedSettings.GetType() == typeof(Settings))
                {
                    return loadedSettings;
                }
            } else
            {
                LogService.Log($"No settings found at '{filePath}'", LogLevel.Warning);
            }
            return null;
        }

        private static void LoadProfiles()
        {
            try
            {
                string profilesDir = Path.Combine(FileService.DataDirectory, ProfilesDirectory);
                Directory.CreateDirectory(profilesDir);
                profiles = Directory
                    .GetFiles(profilesDir, "*.json")
                    .Select(file => new Profile() { Name = Path.GetFileNameWithoutExtension(file), Settings = LoadProfileFromFile(file) })
                    .Where(profile => profile.Settings != null)
                    .ToList();

                LogService.Log($"Loaded {profiles.Count} profiles from '{profilesDir}'", profiles.Count > 0 ? LogLevel.Info : LogLevel.Warning);

                OnUpdate.Invoke(null, EventArgs.Empty);
            } catch(Exception)
            {
                LogService.Log($"Error loading profiles", LogLevel.Error);

            }
        }

        /// <summary>
        /// Override current settings with previously saved profile
        /// </summary>
        /// <param name="name"></param>
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
            catch(Exception) {
                LogService.Log($"Error loading profile '{name}'", LogLevel.Error);
            }
        }

        /// <summary>
        /// Delete a saved profile
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveProfile(string name)
        {
            try
            {
                string file = Path.Combine(FileService.DataDirectory, ProfilesDirectory, name + ".json");
                if (File.Exists(file))
                {
                    File.Delete(file);
                }

                LogService.Log($"Removed profile '{name}'", LogLevel.Info);

                LoadProfiles();
            }
            catch (Exception)
            {
                LogService.Log($"Error removing profile '{name}'", LogLevel.Error);
            }
        }

        /// <summary>
        /// Store current settings as profile on disk
        /// </summary>
        /// <param name="name"></param>
        public static void SaveConfigAsProfile(string name)
        {
            try
            {
                name = new string(name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException();

                SaveConfig(Path.Combine(FileService.DataDirectory, ProfilesDirectory, name + ".json"));

                LogService.Log($"Saved profile '{name}'", LogLevel.Info);

                LoadProfiles();

            } catch(Exception)
            {
                LogService.Log($"Error saving profile '{name}'", LogLevel.Error);
            }
        }
    }
}

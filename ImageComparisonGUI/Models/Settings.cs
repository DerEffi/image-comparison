using Avalonia.Input;
using ImageComparison.Models;
using ImageComparison.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ImageComparisonGUI.Models
{
    public class Settings
    {
        //Auto Processors
        public List<string> AutoProcessors = AutoProcessorService.Supported;
        public int AutoProcessorThreashold = 9900;

        //Cache settings
        public bool CacheNoMatch = true;
        public bool CacheImages = true;

        //Processing settings
        public int MatchThreashold = 7500;
        [JsonConverter(typeof(StringEnumConverter))]
        public HashAlgorithm HashAlgorithm = HashAlgorithm.PHash;
        public int HashDetail = 8;

        //Location settings
        public string[] SearchLocations = Array.Empty<string>();
        [JsonConverter(typeof(StringEnumConverter))]
        public SearchMode SearchMode = SearchMode.All;
        public bool SearchSubdirectories = true;

        //Deletion settings
        [JsonConverter(typeof(StringEnumConverter))]
        public DeleteAction DeleteAction = DeleteAction.RecycleBin;
        public string DeleteTarget = "Duplicates\\";
        public bool RelativeDeleteTarget = true;

        public List<Hotkey> Hotkeys = new() {
            new() {
                Key = Key.S,
                Modifiers = KeyModifiers.None,
                Target = HotkeyTarget.SearchAuto
            },
            new() {
                Key = Key.N,
                Modifiers = KeyModifiers.None,
                Target = HotkeyTarget.SearchNoMatch
            },
            new() {
                Key = Key.A,
                Modifiers = KeyModifiers.Control,
                Target = HotkeyTarget.SearchAbort
            },
            new() {
                Key = Key.Z,
                Modifiers = KeyModifiers.Control,
                Target = HotkeyTarget.SearchPrevious
            },
            new() {
                Key = Key.Enter,
                Modifiers = KeyModifiers.None,
                Target = HotkeyTarget.SearchStart
            },
        };

        /// <summary>
        /// Deepcopy the Settings object
        /// </summary>
        /// <returns>The new unrelated Settings object</returns>
        public Settings Clone()
        {
            this.DistinguishHotkeys();
            return JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(this)) ?? new();
        }

        /// <summary>
        /// Parse string json representation of settings to Settings object
        /// </summary>
        /// <param name="content">Read json string representation of settings from i.e. a file</param>
        /// <returns>Settings object parsed from given content or null</returns>
        public static Settings? Parse(string content)
        {
            try
            {
                Settings? settings = JsonConvert.DeserializeObject<Settings>(content, new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace });
                if (settings != null)
                {
                    settings.DistinguishHotkeys();
                    settings.EnsureAutoProcessors();
                }
                return settings;
            } catch {

                LogService.Log($"Error parsing settings from file content", LogLevel.Error);
            }

            return null;
        }

        /// <summary>
        /// Get settings as text (byte content) for saving
        /// </summary>
        /// <returns>UTF8 encoded text as byte array</returns>
        public byte[] GetContent()
        {
            this.DistinguishHotkeys();
            return new UTF8Encoding(true).GetBytes(JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Removes double Hotkey targets and double key combinations in place
        /// </summary>
        public void DistinguishHotkeys()
        {
            this.Hotkeys.RemoveAll(h => h.Key == Key.None);
            this.Hotkeys = this.Hotkeys.DistinctBy(h => h.Target).DistinctBy(h => $"{h.Modifiers}-{h.Key}").ToList();
        }

        /// <summary>
        /// Removes unsupported Processors and adds missing ones
        /// </summary>
        public void EnsureAutoProcessors()
        {
            this.AutoProcessors.RemoveAll(p => !AutoProcessorService.Supported.Any(s => s == p));
            if(!this.AutoProcessors.Any(p => p == "None"))
                this.AutoProcessors.Add("None");
            AutoProcessorService.Supported.ForEach(supported =>
            {
                if (!this.AutoProcessors.Any(p => p == supported))
                    this.AutoProcessors.Add(supported);
            });
        }
    }
}

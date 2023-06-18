using ImageComparison.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace ImageComparisonGUI.Models
{
    public class Settings
    {
        //Processing settings
        public int MatchThreashold = 7500;
        public int HashDetail = 20;
        public bool HashBothDirections = true;

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

        public Settings Clone()
        {
            return JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(this)) ?? new();
        }
    }
}

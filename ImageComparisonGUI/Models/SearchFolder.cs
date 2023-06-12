using System.Collections.Generic;

namespace ImageComparisonGUI.Models
{
    public class SearchFolder
    {
        public string Path = "";
        public List<string> Files = new();
        public List<SearchFolder> Folders = new();
    }
}

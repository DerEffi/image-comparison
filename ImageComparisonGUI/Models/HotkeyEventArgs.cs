using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparisonGUI.Models
{
    public class HotkeyEventArgs
    {
        public Hotkey PressedHotkey = new();
        public string SelectedPage = "";
    }
}

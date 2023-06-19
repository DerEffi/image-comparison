using Avalonia.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparisonGUI.Models
{
    public class Hotkey
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Key Key = Key.None;

        [JsonConverter(typeof(StringEnumConverter))]
        public KeyModifiers Modifiers = KeyModifiers.None;

        [JsonConverter(typeof(StringEnumConverter))]
        public HotkeyTarget Target = HotkeyTarget.None;

        public Hotkey Clone()
        {
            return (Hotkey)MemberwiseClone();
        }
    }
}

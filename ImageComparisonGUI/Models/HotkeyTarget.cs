using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparisonGUI.Models
{
    public enum HotkeyTarget
    {
        None,
        SearchNoMatch,
        SearchPrevious,
        SearchDeleteLeft,
        SearchDeleteRight,
        SearchDeleteBoth,
        SearchAuto,
        AutoProcessAll,
        SearchAbort,
        SearchStart
    }
}

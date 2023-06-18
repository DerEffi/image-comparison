using Avalonia.Controls;
using Avalonia.Input;
using ImageComparisonGUI.Models;
using System;
using System.Linq;

namespace ImageComparisonGUI.Services
{
    public static class HotkeyService
    {
        public static event EventHandler<HotkeyEventArgs> OnHotkey = delegate { };
        private static string selectedPage = "";

        public static void OnKeyInput(object? sender, KeyEventArgs e)
        {
            Hotkey? hotkey = ConfigService.Hotkeys.FirstOrDefault(h => h.Modifiers == e.KeyModifiers && h.Key == e.Key);

            if (hotkey != null)
            {
                OnHotkey(null, new()
                {
                    PressedHotkey = hotkey.Target,
                    SelectedPage = selectedPage
                });
            }
        }

        public static void OnPageSelection(object? sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count != 0 && e.AddedItems[0] is TabItem tabItem)
            {
                selectedPage = tabItem.Header?.ToString() ?? "";
            } else
            {
                selectedPage = "";
            }
        }

        public static void OnPageSelection(object? tab)
        {
            if(tab is TabItem tabItem)
                selectedPage = tabItem.Header?.ToString() ?? "";
        }
    }
}

using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparisonGUI.Models;
using ImageComparisonGUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparisonGUI.ViewModels
{
    public partial class HotkeysPageViewModel : ViewModelBase
    {
        [ObservableProperty] private AvaloniaList<Hotkey> hotkeys = new(ConfigService.Hotkeys);
        [ObservableProperty] private HotkeyTarget listenTarget = HotkeyTarget.None;

        public HotkeysPageViewModel() {
            ConfigService.OnUpdate += OnConfigUpdate;
            HotkeyService.OnHotkey += OnKeyInput;
        }

        public void Save()
        {
            
        }

        public void OnKeyInput(object? sender, HotkeyEventArgs e)
        {
            if (ListenTarget != HotkeyTarget.None)
            {
                if (e.PressedHotkey.Key == Avalonia.Input.Key.Escape)
                {
                    ListenTarget = HotkeyTarget.None;
                }
                else if (e.PressedHotkey.Key != Avalonia.Input.Key.None)
                {
                    List<Hotkey> hotkeyList = Hotkeys.ToList();
                    Hotkey? hotkey = hotkeyList.FirstOrDefault(h => h.Target == ListenTarget);
                    if (hotkey == null) {
                        hotkey = new Hotkey() { Target = ListenTarget };
                        hotkeyList.Add(hotkey);
                    }
                    hotkey.Key = e.PressedHotkey.Key;
                    hotkey.Modifiers = e.PressedHotkey.Modifiers;
                    
                    ListenTarget = HotkeyTarget.None;

                    Hotkeys = new(hotkeyList);
                    ConfigService.UpdateHotkeys(Hotkeys.ToList());
                }
            }
        }

        public void OnConfigUpdate(object? sender, EventArgs e)
        {
            Hotkeys = new(ConfigService.Hotkeys);
        }

        [RelayCommand]
        private void Remove(string? needle)
        {
            HotkeyTarget target;
            if (Enum.TryParse(needle, out target))
            {
                List<Hotkey> hotkeyList = Hotkeys.ToList();
                Hotkey? hotkey = hotkeyList.FirstOrDefault(h => h.Target == target);
                if (hotkey != null)
                {
                    hotkeyList.Remove(hotkey);
                    Hotkeys = new(hotkeyList);
                    ConfigService.UpdateHotkeys(Hotkeys.ToList());
                }
            }
        }

        [RelayCommand]
        private void ListenForHotkey(string needle)
        {
            HotkeyTarget target;
            if (Enum.TryParse(needle, out target))
            {
                ListenTarget = target;
            } else
            {
                ListenTarget = HotkeyTarget.None;
            }
        }
    }
}

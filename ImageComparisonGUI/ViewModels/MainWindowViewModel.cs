using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.Views;
using SkiaSharp;
using System;
using System.Reflection;

namespace ImageComparisonGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private double tabWidth = 0;
    [ObservableProperty]
    private double tabHeight = 0;

    public MainWindowViewModel(MainWindow window, DirectProperty<TopLevel, Size> ClientSizeProperty)
    {
        ConfigService.Init();
        CacheService.Init();

        ClientSizeProperty.Changed.Subscribe(size => Resize(size.NewValue.Value.Width, size.NewValue.Value.Height));
        window.Opened += (object? sender, EventArgs e) => { Resize(window.ClientSize.Width, window.ClientSize.Height); };
        window.KeyDown += HotkeyService.OnKeyInput;
        TabControl tabs = window.Find<TabControl>("TabControl");
        if (tabs != null)
        {
            HotkeyService.OnPageSelection(tabs.SelectedItem);
            tabs.SelectionChanged += HotkeyService.OnPageSelection;
        }
    }

    public void Resize(double width, double height)
    {
        TabWidth = width - 300;
        TabHeight = height - 140;
    }
}

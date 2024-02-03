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

        // Trigger resizing of tab content when user resizes window
        ResizeObserver resizeObserver = new ResizeObserver();
        resizeObserver.OnUpdate += Resize;
        ClientSizeProperty.Changed.Subscribe(resizeObserver);
        window.Opened += (object? sender, EventArgs e) => { Resize(window.ClientSize.Width, window.ClientSize.Height); };
        
        // change hotkey target when opening new tab
        window.KeyDown += HotkeyService.OnKeyInput;
        TabControl tabs = window.Find<TabControl>("TabControl");
        if (tabs != null)
        {
            HotkeyService.OnPageSelection(tabs.SelectedItem);
            tabs.SelectionChanged += HotkeyService.OnPageSelection;
        }
    }

    private void Resize(object? sender, ResizeEventArgs e)
    {
        Resize(e.Size.Width, e.Size.Height);
    }

    private void Resize(double width, double height)
    {
        TabWidth = width - 302;
        TabHeight = height - 142;
    }
}

public class ResizeObserver : IObserver<AvaloniaPropertyChangedEventArgs<Size>>
{
    public event EventHandler<ResizeEventArgs> OnUpdate = delegate { };

    public void OnCompleted() { }
    public void OnError(Exception e) { }
    public void OnNext(AvaloniaPropertyChangedEventArgs<Size> size)
    {
        OnUpdate.Invoke(null, new() { Size = size.NewValue.Value });
    }
}

public class ResizeEventArgs : EventArgs
{
    public Size Size { get; set; }
}

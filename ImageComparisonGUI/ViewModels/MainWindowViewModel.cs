using CommunityToolkit.Mvvm.ComponentModel;
using ImageComparisonGUI.Services;
using SkiaSharp;
using System.Reflection;

namespace ImageComparisonGUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private double tabWidth = 0;
    [ObservableProperty]
    private double tabHeight = 0;

    public MainWindowViewModel(double width, double height)
    {
        Resize(width, height);
    }

    public void Resize(double width, double height)
    {
        TabWidth = width - 300;
        TabHeight = height - 140;
    }
}

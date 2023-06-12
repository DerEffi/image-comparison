using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparisonGUI.Services;
using System;
using System.Reflection;

namespace ImageComparisonGUI.ViewModels;

public partial class AboutPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string versionText = "0.0.0.0";

    public AboutPageViewModel()
    {
        AssemblyName name = Assembly.GetExecutingAssembly().GetName();
        versionText = $"{name.Name} - Version {name.Version}";
    }

    [RelayCommand]
    private static void OpenHyperlink(string url)
    {
        try
        {
            UrlService.OpenUrl(url);
        }
        catch (Exception) { }
    }
}

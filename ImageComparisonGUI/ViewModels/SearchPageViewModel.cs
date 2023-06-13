using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparisonGUI.Services;
using System.Diagnostics;
using System.IO;

namespace ImageComparisonGUI.ViewModels;

public partial class SearchPageViewModel : ViewModelBase
{
    #region Observables

    [ObservableProperty] private FileInfo? leftImage;
    [ObservableProperty] private FileInfo? rightImage;
    [ObservableProperty] private bool idle = true;
    [ObservableProperty] private bool searching = false;
    [ObservableProperty] private bool displaying = false;
    [ObservableProperty] private string statusText = "Showing Results: ";
    [ObservableProperty] private string imageCountText = "107 / 328";
    [ObservableProperty] private int percentComplete = 33;

    #endregion

    public SearchPageViewModel(Button leftImageButton, Button rightImageButton)
    {
        leftImageButton.DoubleTapped += (object? sender, RoutedEventArgs e) => OpenImage(LeftImage != null ? LeftImage.FullName : null);
        rightImageButton.DoubleTapped += (object? sender, RoutedEventArgs e) => OpenImage(RightImage != null ? RightImage.FullName : null);
    }

    #region Commands

    [RelayCommand]
    private void DeleteImage(int side)
    {
        try
        {
            if (side <= 0 && LeftImage != null)
                FileService.DeleteFile(LeftImage.FullName);

            if (side >= 0 && RightImage != null)
                FileService.DeleteFile(RightImage.FullName);
        } catch { }

        NextPair();
    }

    [RelayCommand]
    public void NoMatch()
    {
        NextPair();
    }

    [RelayCommand]
    public void Search()
    {

    }

    [RelayCommand]
    public void Abort()
    {

    }

    [RelayCommand]
    public void AutoProcess()
    {
    
    }

    [RelayCommand]
    public void OpenExplorer(string path)
    {
        if(File.Exists(path))
        {
            Process.Start("explorer.exe", $"/select, \"{path}\"");
        } else
        {
            string? directory = Path.GetDirectoryName(path);
            if (directory != null && Directory.Exists(directory))
                Process.Start("explorer", directory);
        }
    }

    public void OpenImage(string? path)
    {
        if (File.Exists(path))
            Process.Start("explorer", $"\"{path}\"");
    }

    #endregion

    #region Data Functions

    private void NextPair()
    {

    }

    #endregion
}

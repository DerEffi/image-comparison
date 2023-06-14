using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageComparisonGUI.ViewModels;

public partial class SearchPageViewModel : ViewModelBase
{
    #region Observables

    [ObservableProperty] private FileInfo? leftImage;
    [ObservableProperty] private FileInfo? rightImage;
    [ObservableProperty] private bool idle = true;
    [ObservableProperty] private bool searching = false;
    [ObservableProperty] private bool displaying = false;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private string imageCountText = "";
    [ObservableProperty] private int percentComplete = 0;

    #endregion

    public SearchPageViewModel(Button leftImageButton, Button rightImageButton)
    {
        CompareService.OnProgress += OnProgress;
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
        Task.Run(() =>
        {
            Idle = false;
            Searching = true;
            StatusText = "Searching";

            List<List<FileInfo>> folders = FileService.GetProcessableFiles(ConfigService.SearchLocations, ConfigService.SearchSubdirectories);

            Searching = false;
            StatusText = "Analysing";
            PercentComplete = 0;

            var matches = CompareService.GetMatches(folders, SearchMode.All);
        });
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

    public void OnProgress(object? sender, ImageComparerEventArgs e)
    {
        PercentComplete = Convert.ToInt32((decimal.Divide(e.Current, e.Target) * 100));
        ImageCountText = $"{e.Current} / {e.Target}";
    }

    #endregion

    #region Data Functions

    private void NextPair()
    {

    }

    #endregion
}

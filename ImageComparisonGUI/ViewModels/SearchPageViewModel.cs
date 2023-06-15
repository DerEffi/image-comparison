using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emgu.CV.Dnn;
using ImageComparison.Models;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageComparisonGUI.ViewModels;

public partial class SearchPageViewModel : ViewModelBase
{
    private CancellationTokenSource ComparerTaskToken = new();

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
        try {
            Task.Run(() =>
            {
                Idle = false;
                Searching = true;
                StatusText = "Searching";

                List<List<FileInfo>> searchLocations = FileService.GetProcessableFiles(ConfigService.SearchLocations, ConfigService.SearchSubdirectories);

                Searching = true;
                StatusText = "Analysing";
                PercentComplete = 0;

                List<List<ImageAnalysis>> analysedImages = CompareService.AnalyseImages(searchLocations, ComparerTaskToken.Token);

                Searching = true;
                StatusText = "Comparing";
                imageCountText = "";
                PercentComplete = 0;

                List<ImageMatch> matches = CompareService.SearchForDuplicates(analysedImages, ConfigService.SearchMode, ComparerTaskToken.Token);

                ComparerTaskToken.Dispose();
                ComparerTaskToken = new();
            });
        } catch (Exception) { }
    }

    [RelayCommand]
    public void Abort()
    {
        Searching = false;
        StatusText = "";
        ImageCountText = "";
        PercentComplete = 0;
        Idle = true;

        ComparerTaskToken.Cancel();
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
        if(e.Target > 0)
            PercentComplete = Convert.ToInt32((decimal.Divide(e.Current, e.Target) * 100));
        ImageCountText = $"{e.Current} / {e.Target}";
        Searching = false;
    }

    #endregion

    #region Data Functions

    private void NextPair()
    {

    }

    #endregion
}

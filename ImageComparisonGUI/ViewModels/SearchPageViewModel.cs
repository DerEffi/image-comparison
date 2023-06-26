using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparison.Services;
using ImageComparisonGUI.Models;
using ImageComparisonGUI.Pages;
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
    private Task? ComparerTask = null;
    private List<ImageMatch> Matches = new();
    private int displayedMatchIndex = 0;

    #region Observables

    [ObservableProperty] private ImageMatch displayedMatch = new();
    [ObservableProperty] private bool idle = true;
    [ObservableProperty] private bool searching = false;
    [ObservableProperty] private bool displaying = false;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private string imageCountText = "";
    [ObservableProperty] private int percentComplete = 0;

    #endregion

    public SearchPageViewModel(SearchPage userControl)
    {
        CompareService.OnProgress += OnProgress;

        Button leftImageButton = userControl.Find<Button>("LeftImageButton");
        if (leftImageButton != null)
            leftImageButton.DoubleTapped += (object? sender, RoutedEventArgs e) => OpenImage(DisplayedMatch.Image1?.Image.FullName);
        
        Button rightImageButton = userControl.Find<Button>("RightImageButton");
        if(rightImageButton != null)
            rightImageButton.DoubleTapped += (object? sender, RoutedEventArgs e) => OpenImage(DisplayedMatch.Image2?.Image.FullName);
        
        Button searchButton = userControl.Find<Button>("SearchButton");
        if(searchButton != null)
            searchButton.Click += Search;

        HotkeyService.OnHotkey += OnHotkey;
    }

    #region RelayCommands

    [RelayCommand]
    private void DeleteImage(int side)
    {
        try
        {
            if (side <= 0 && DisplayedMatch.Image1 != null)
                DeleteFile(DisplayedMatch.Image1.Image.FullName);

            if (side >= 0 && DisplayedMatch.Image2 != null)
                DeleteFile(DisplayedMatch.Image2.Image.FullName);
        } catch { }

        NextPair();
    }

    [RelayCommand]
    public void NoMatch()
    {
        if(ConfigService.CacheNoMatch)
            CacheService.AddNoMatch(displayedMatch.Image1.Image.FullName, displayedMatch.Image2.Image.FullName);
        NextPair();
    }

    [RelayCommand]
    public void Previous()
    {
        if (displayedMatchIndex > 0)
        {
            displayedMatchIndex--;
            DisplayedMatch = Matches[displayedMatchIndex];
            ImageCountText = $"{displayedMatchIndex + 1} / {Matches.Count}";
        }
    }

    [RelayCommand]
    public void Abort()
    {
        if(ComparerTask != null && !ComparerTask.IsCompleted)
        {
            ComparerTaskToken.Cancel();
        } else
        {
            ResetUI();
        }
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

    #endregion

    #region Data Functions

    public void OnHotkey(object? sender, HotkeyEventArgs e)
    {
        if(e.SelectedPage == "Search")
        {
            try
            {
                switch (e.PressedHotkey.Target)
                {
                    case HotkeyTarget.SearchStart:
                        Search(null, new());
                        break;
                    case HotkeyTarget.SearchAbort:
                        Abort();
                        break;
                    case HotkeyTarget.SearchPrevious:
                        Previous();
                        break;
                    case HotkeyTarget.SearchNoMatch:
                        NoMatch();
                        break;
                    case HotkeyTarget.SearchDeleteLeft:
                        DeleteImage(-1);
                        break;
                    case HotkeyTarget.SearchDeleteRight:
                        DeleteImage(1);
                        break;
                    case HotkeyTarget.SearchDeleteBoth:
                        DeleteImage(0);
                        break;
                    case HotkeyTarget.SearchAuto:
                        AutoProcess();
                        break;
                }
            } catch { }
        }
    }

    public void Search(object? sender, RoutedEventArgs e)
    {
        if (ComparerTask != null && !ComparerTask.IsCompleted)
            throw new InvalidOperationException();

        ComparerTask = Task.Run(() =>
        {
            Idle = false;
            Searching = true;
            StatusText = "Searching";
            ImageCountText = "";
            ConfigService.Lock();

            string hashVersion = $"V{HashService.Version}D{ConfigService.HashDetail}{(ConfigService.HashBothDirections ? "B" : "U")}";
            ulong scantime = (ulong)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

            List<List<FileInfo>> searchLocations = FileService.GetProcessableFiles(ConfigService.SearchLocations, ConfigService.SearchSubdirectories);

            Searching = true;
            StatusText = "Analysing";
            PercentComplete = 0;

            List<CacheItem> cachedAnalysis = ConfigService.CacheImages ? CacheService.GetImages(hashVersion) : new();
            List<List<ImageAnalysis>> analysedImages = CompareService.AnalyseImages(searchLocations, ConfigService.HashDetail, ConfigService.HashBothDirections, cachedAnalysis, ComparerTaskToken.Token);
            if(ConfigService.CacheImages)
                CacheService.UpdateImages(analysedImages.SelectMany(i => i).ToList(), hashVersion, scantime);

            Searching = true;
            StatusText = "Comparing";
            ImageCountText = "";
            PercentComplete = 0;

            List<NoMatch> nomatches = ConfigService.CacheNoMatch ? CacheService.GetNoMatches() : new();
            Matches = CompareService.SearchForDuplicates(analysedImages, ConfigService.MatchThreashold, ConfigService.SearchMode, nomatches, ComparerTaskToken.Token);

            displayedMatchIndex = 0;
            if(ConfigService.FillNoMatchCache)
            {
                CacheService.AddNoMatches(Matches);
                ConfigService.UpdateCache(ConfigService.CacheImages, ConfigService.CacheNoMatch, false);
                ResetUI();
            }
            else if (Matches != null && Matches.Count > 0)
            {
                DisplayedMatch = Matches.First();
                StatusText = "Showing Matches: ";
                ImageCountText = $"1 / {Matches.Count}";
                Displaying = true;
            }
            else
            {
                ResetUI();
            }
            Searching = false;

        }, ComparerTaskToken.Token)
        .ContinueWith(task =>
        {
            ResetUI();

            ComparerTaskToken.Dispose();
            ComparerTaskToken = new();
            GC.Collect();
        }, TaskContinuationOptions.OnlyOnCanceled)
        .ContinueWith(t => { });
    }

    public void OnProgress(object? sender, ImageComparerEventArgs e)
    {
        if (e.Target > 0)
            PercentComplete = Convert.ToInt32((decimal.Divide(e.Current, e.Target) * 100));
        ImageCountText = $"{e.Current} / {e.Target}";
        Searching = false;
    }

    private void NextPair()
    {
        if(Matches.Count > ++displayedMatchIndex)
        {
            DisplayedMatch = Matches[displayedMatchIndex];
            ImageCountText = $"{displayedMatchIndex + 1} / {Matches.Count}";
        } else
        {
            ResetUI();
        }
    }

    public void ResetUI()
    {
        Matches = new();
        DisplayedMatch = new();
        displayedMatchIndex = 0;
        Searching = false;
        Displaying = false;
        StatusText = "";
        ImageCountText = "";
        PercentComplete = 0;
        Idle = true;
        ConfigService.Unlock();
    }

    public void OpenImage(string? path)
    {
        if (File.Exists(path))
            Process.Start("explorer", $"\"{path}\"");
    }

    private void DeleteFile(string path)
    {
        try
        {
            FileService.DeleteFile(path, ConfigService.DeleteAction, ConfigService.DeleteTarget, ConfigService.RelativeDeleteTarget);
            List<ImageMatch> validMatches = Matches.GetRange(0, displayedMatchIndex + 1);
            for(int i = displayedMatchIndex + 1; i < Matches.Count; i++)
            {
                if(Matches[i].Image1.Image.FullName != path && Matches[i].Image2.Image.FullName != path)
                    validMatches.Add(Matches[i]);
            }
            Matches = new(validMatches);
        }
        catch { }
    }

    #endregion
}

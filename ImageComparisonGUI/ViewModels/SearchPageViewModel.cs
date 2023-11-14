using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparison.Services;
using ImageComparisonGUI.Models;
using ImageComparisonGUI.Pages;
using ImageComparisonGUI.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageComparisonGUI.ViewModels;

public partial class SearchPageViewModel : ViewModelBase
{
    private CancellationTokenSource ComparerTaskToken = new();
    private Task? ComparerTask = null;
    private CancellationTokenSource AutoProcessingTaskToken = new();
    private Task? AutoProcessingTask = null;
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
    [ObservableProperty] private int autoProcessSide = 0;
    [ObservableProperty] private string autoProcessProperty = "None";

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
            CacheService.AddNoMatch(DisplayedMatch.Image1.Image.FullName, DisplayedMatch.Image2.Image.FullName);

        NextPair();
    }

    [RelayCommand]
    public void Previous()
    {
        NextPair(false);
    }

    [RelayCommand]
    public void Abort()
    {
        if(AutoProcessingTask != null && !AutoProcessingTask.IsCompleted)
        {
            AutoProcessingTaskToken.Cancel();
        } else if(ComparerTask != null && !ComparerTask.IsCompleted)
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
        if(AutoProcessProperty != null && AutoProcessProperty != "" && AutoProcessProperty != "None" && AutoProcessSide != 0)
        {
            DeleteImage(AutoProcessSide);
        } else
        {
            NoMatch();
        }
    }

    [RelayCommand]
    public void AutoProcessAll()
    {
        if (AutoProcessingTask != null && !AutoProcessingTask.IsCompleted)
            throw new InvalidOperationException();

        AutoProcessingTask = Task.Run(() =>
        {
            Displaying = false;
            StatusText = "Auto Processing";
            while(displayedMatchIndex < Matches.Count)
            {
                DisplayedMatch = Matches[displayedMatchIndex];
                ImageCountText = $"{displayedMatchIndex + 1} / {Matches.Count}";
                PreviewAutoProcessor();

                if (AutoProcessProperty != null && AutoProcessProperty != "" && AutoProcessProperty != "None" && AutoProcessSide != 0)
                {
                    try
                    {
                        if (AutoProcessSide < 0 && DisplayedMatch.Image1 != null)
                            DeleteFile(DisplayedMatch.Image1.Image.FullName);

                        if (AutoProcessSide > 0 && DisplayedMatch.Image2 != null)
                            DeleteFile(DisplayedMatch.Image2.Image.FullName);
                    }
                    catch { }
                }
                else
                {
                    if (ConfigService.CacheNoMatch)
                        CacheService.AddNoMatch(DisplayedMatch.Image1.Image.FullName, DisplayedMatch.Image2.Image.FullName);
                }
                Thread.Sleep(1000);
                
                displayedMatchIndex++;
                AutoProcessingTaskToken.Token.ThrowIfCancellationRequested();
            }
        }, AutoProcessingTaskToken.Token)
        .ContinueWith(task =>
        {
            AutoProcessingTaskToken.Dispose();
            AutoProcessingTaskToken = new();
            GC.Collect();
        }, TaskContinuationOptions.OnlyOnCanceled)
        .ContinueWith(t => {
            Displaying = true;
            StatusText = "Showing Matches: ";
            displayedMatchIndex--;
            NextPair();
        });
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
                    case HotkeyTarget.AutoProcessAll:
                        AutoProcessAll();
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

            ComparerTaskToken.Token.ThrowIfCancellationRequested();

            List<List<FileInfo>> searchLocations = FileService.GetProcessableFiles(ConfigService.SearchLocations, ConfigService.SearchSubdirectories);

            ComparerTaskToken.Token.ThrowIfCancellationRequested();

            Searching = true;
            StatusText = "Analysing";
            PercentComplete = 0;

            List<CacheItem> cachedAnalysis = ConfigService.CacheImages ? CacheService.GetImages(hashVersion) : new();
            List<List<ImageAnalysis>> analysedImages = CompareService.AnalyseImages(searchLocations, ConfigService.HashDetail, ConfigService.HashBothDirections, cachedAnalysis, ComparerTaskToken.Token);
            if(ConfigService.CacheImages)
                CacheService.UpdateImages(analysedImages.SelectMany(i => i).ToList(), hashVersion, scantime);

            ComparerTaskToken.Token.ThrowIfCancellationRequested();

            Searching = true;
            StatusText = "Comparing";
            ImageCountText = "";
            PercentComplete = 0;

            List<NoMatch> nomatches = ConfigService.CacheNoMatch ? CacheService.GetNoMatches() : new();
            Matches = CompareService.SearchForDuplicates(analysedImages, ConfigService.MatchThreashold, ConfigService.SearchMode, nomatches, ComparerTaskToken.Token);

            ComparerTaskToken.Token.ThrowIfCancellationRequested();

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
                PreviewAutoProcessor();
                Displaying = true;
            }
            else
            {
                ResetUI();
            }
            Searching = false;

            ComparerTaskToken.Token.ThrowIfCancellationRequested();

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

    private void NextPair(bool forward = true)
    {
        if(
            (forward && Matches.Count > ++displayedMatchIndex)
            || (!forward && --displayedMatchIndex >= 0)
        )
        {
            DisplayedMatch = Matches[displayedMatchIndex];
            ImageCountText = $"{displayedMatchIndex + 1} / {Matches.Count}";

            PreviewAutoProcessor();
        } else
        {
            ResetUI();
        }
    }

    public void PreviewAutoProcessor()
    {
        try
        {
            List<string> autoProcessors = ConfigService.AutoProcessors;
            int currentProcessor = 0;
            while (currentProcessor < autoProcessors.Count)
            {
                int processingResult = AutoProcessorService.Processors.First(p => p.DisplayName == autoProcessors[currentProcessor]).Process(DisplayedMatch.Image1.Image, DisplayedMatch.Image2.Image);
                if (processingResult != 0)
                {
                    AutoProcessSide = processingResult;
                    AutoProcessProperty = autoProcessors[currentProcessor];
                    break;
                }
                currentProcessor++;
            }

            if (currentProcessor >= autoProcessors.Count)
                throw new OperationCanceledException();
        }
        catch (Exception)
        {
            AutoProcessSide = 0;
            AutoProcessProperty = "None";
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
        AutoProcessSide = 0;
        AutoProcessProperty = "None";
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

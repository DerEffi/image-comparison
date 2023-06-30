using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using System;
using System.Reflection;

namespace ImageComparisonGUI.ViewModels;

public partial class CachePageViewModel : ViewModelBase
{
    [ObservableProperty] private bool cacheImages = ConfigService.CacheImages;
    [ObservableProperty] private bool cacheNoMatch = ConfigService.CacheNoMatch;
    [ObservableProperty] private bool fillNoMatchCache = ConfigService.FillNoMatchCache;
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;

    public CachePageViewModel()
    {
        ConfigService.OnUpdate += OnConfigUpdate;
    }

    [RelayCommand]
    public void ClearImageCache()
    {
        CacheService.ClearImageCache();
    }

    [RelayCommand]
    public void ClearNoMatchCache()
    {
        CacheService.ClearNoMatchCache();
    }

    [RelayCommand]
    public void Save()
    {
        ConfigService.UpdateCache(CacheImages, CacheNoMatch, FillNoMatchCache);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        CacheImages = ConfigService.CacheImages;
        CacheNoMatch = ConfigService.CacheNoMatch;
        FillNoMatchCache = ConfigService.FillNoMatchCache;
        ConfigLocked = ConfigService.IsLocked;
    }
}

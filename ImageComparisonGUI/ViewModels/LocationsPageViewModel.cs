using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.Views;
using System;
using System.Linq;

namespace ImageComparisonGUI.ViewModels;

public partial class LocationsPageViewModel : ViewModelBase
{
    [ObservableProperty] private AvaloniaList<string> searchLocations = new(ConfigService.SearchLocations);
    [ObservableProperty] private int? selectedSearchLocation = null;
    [ObservableProperty] private SearchMode selectedSearchMode = ConfigService.SearchMode;
    [ObservableProperty] private bool recursive = ConfigService.SearchSubdirectories;
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;

    public SelectionModel<string> SearchLocationsSelection { get; } = new();

    public LocationsPageViewModel()
    {
        SearchLocationsSelection.SelectionChanged += SearchLocationsSelectionChanged;

        ConfigService.OnUpdate += OnConfigUpdate;
    }

    private void SearchLocationsSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        SelectedSearchLocation = e.SelectedItems.Count != 0 ? e.SelectedIndexes[0] : null;
    }

    [RelayCommand]
    public void RemoveSearchLocation()
    {
        if (SelectedSearchLocation != null)
        {
            SearchLocations.RemoveAt(SelectedSearchLocation ?? 0);
            ConfigService.UpdateSearchLocations(SelectedSearchMode, SearchLocations.ToArray(), recursive);
        }
    }

    [RelayCommand]
    public async void AddSearchLocation()
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        string? location = await dialog.ShowAsync(MainWindow.Instance);
        if (location != null && !SearchLocations.Contains(location))
        {
            SearchLocations.Add(location);
            ConfigService.UpdateSearchLocations(SelectedSearchMode, SearchLocations.ToArray(), recursive);
        }
    }

    [RelayCommand]
    public void Save()
    {
        ConfigService.UpdateSearchLocations(SelectedSearchMode, SearchLocations.ToArray(), recursive);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        SearchLocations = new(ConfigService.SearchLocations);
        SelectedSearchLocation = null;
        SelectedSearchMode = ConfigService.SearchMode;
        Recursive = ConfigService.SearchSubdirectories;
        ConfigLocked = ConfigService.IsLocked;
    }
}

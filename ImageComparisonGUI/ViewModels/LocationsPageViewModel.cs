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
    [ObservableProperty] private AvaloniaList<string> searchFolders = new(ConfigService.SearchLocations);
    [ObservableProperty] private int? selectedSearchFolder = null;
    [ObservableProperty] private SearchMode selectedSearchMode = ConfigService.SearchMode;
    [ObservableProperty] private bool recursive = ConfigService.SearchSubdirectories;

    public SelectionModel<string> SearchFoldersSelection { get; }

    public LocationsPageViewModel()
    {
        SearchFoldersSelection = new();
        SearchFoldersSelection.SelectionChanged += SearchFoldersSelectionChanged;

        ConfigService.OnUpdate += OnConfigUpdate;
    }

    private void SearchFoldersSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        SelectedSearchFolder = e.SelectedItems.Count != 0 ? e.SelectedIndexes[0] : null;
    }

    [RelayCommand]
    public void RemoveSearchFolder()
    {
        if (SelectedSearchFolder != null)
            SearchFolders.RemoveAt(SelectedSearchFolder ?? 0);
    }

    [RelayCommand]
    public async void AddSearchFolder()
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        string? folder = await dialog.ShowAsync(MainWindow.Instance);
        if(folder != null && !SearchFolders.Contains(folder))
            SearchFolders.Add(folder);
    }

    [RelayCommand]
    public void Save()
    {
        ConfigService.UpdateSearchLocations(SelectedSearchMode, SearchFolders.ToArray(), recursive);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        SearchFolders = new(ConfigService.SearchLocations);
        SelectedSearchFolder = null;
        SelectedSearchMode = ConfigService.SearchMode;
        Recursive = ConfigService.SearchSubdirectories;
    }
}

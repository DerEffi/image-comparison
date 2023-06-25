using Avalonia.Collections;
using Avalonia.Controls.Selection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using System;
using System.Diagnostics;
using System.IO;

namespace ImageComparisonGUI.ViewModels;

public partial class ProfilesPageViewModel : ViewModelBase
{
    [ObservableProperty] private AvaloniaList<string> profiles = new(ConfigService.Profiles);
    [ObservableProperty] private int? selectedProfile = null;
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;
    [ObservableProperty] private string newProfileName = "";

    public SelectionModel<string> ProfilesSelection { get; } = new();

    public ProfilesPageViewModel()
    {
        ProfilesSelection.SelectionChanged += ProfilesSelectionChanged;

        ConfigService.OnUpdate += OnConfigUpdate;
    }

    private void ProfilesSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        SelectedProfile = e.SelectedItems.Count != 0 ? e.SelectedIndexes[0] : null;

        if(SelectedProfile != null && SelectedProfile < profiles.Count)
        {
            NewProfileName = Profiles[(int)SelectedProfile];
        }
    }

    [RelayCommand]
    public void RemoveProfile()
    {
        if (SelectedProfile != null && SelectedProfile < profiles.Count)
        {
            ConfigService.RemoveProfile(Profiles[(int)SelectedProfile]);
        }
    }

    [RelayCommand]
    public void AddProfile()
    {
        if (!string.IsNullOrEmpty(NewProfileName))
        {
            ConfigService.SaveConfigAsProfile(NewProfileName);
        }
    }

    [RelayCommand]
    public void LoadProfile()
    {
        if (SelectedProfile != null && SelectedProfile < profiles.Count)
        {
            ConfigService.LoadProfile(Profiles[(int)SelectedProfile]);
        }
    }

    [RelayCommand]
    public void OpenProfileDirectory()
    {
        string profileDirectory = Path.Combine(FileService.DataDirectory, ConfigService.ProfilesDirectory);
        if(!Directory.Exists(profileDirectory))
            Directory.CreateDirectory(profileDirectory);    
        Process.Start("explorer", $"\"{profileDirectory}\"");
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        Profiles = new(ConfigService.Profiles); 
        ConfigLocked = ConfigService.IsLocked;
    }
}

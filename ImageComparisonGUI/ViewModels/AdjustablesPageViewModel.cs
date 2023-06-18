using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.Views;
using System;
using System.Linq;

namespace ImageComparisonGUI.ViewModels;

public partial class AdjustablesPageViewModel : ViewModelBase
{
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;
    [ObservableProperty] private bool hashBothDirections = ConfigService.HashBothDirections;
    [ObservableProperty] private int hashDetail = ConfigService.HashDetail;
    [ObservableProperty] private int matchThreashold = ConfigService.MatchThreashold;

    public AdjustablesPageViewModel(Slider matchThreasholdSlider, Slider hashDetailSlider)
    {
        matchThreasholdSlider.LostFocus += (object? sender, RoutedEventArgs e) => Save();
        hashDetailSlider.LostFocus += (object? sender, RoutedEventArgs e) => Save();

        ConfigService.OnUpdate += OnConfigUpdate;
    }

    [RelayCommand]
    private void Save()
    {
        MatchThreashold -= MatchThreashold % 10;
        ConfigService.UpdateAdjustables(MatchThreashold, HashDetail, HashBothDirections);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        HashBothDirections = ConfigService.HashBothDirections;
        HashDetail = ConfigService.HashDetail;
        MatchThreashold = ConfigService.MatchThreashold;
        ConfigLocked = ConfigService.IsLocked;
    }
}

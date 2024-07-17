using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparison.Models;
using ImageComparisonGUI.Models;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.Views;
using System;
using System.Linq;

namespace ImageComparisonGUI.ViewModels;

public partial class AdjustablesPageViewModel : ViewModelBase
{
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;
    [ObservableProperty] private HashAlgorithm hashAlgorithm = ConfigService.HashAlgorithm;
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
        MatchThreashold = (int)(Math.Round((double)MatchThreashold / 10) * 10); //rounding to the nearest 10
        ConfigService.UpdateAdjustables(MatchThreashold, HashDetail, HashAlgorithm);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        HashAlgorithm = ConfigService.HashAlgorithm;
        HashDetail = ConfigService.HashDetail;
        MatchThreashold = ConfigService.MatchThreashold;
        ConfigLocked = ConfigService.IsLocked;
    }
}

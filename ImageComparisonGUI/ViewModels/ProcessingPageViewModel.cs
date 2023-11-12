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

public partial class ProcessingPageViewModel : ViewModelBase
{
    [ObservableProperty] private AvaloniaList<string> processors = new(ConfigService.MatchProcessors);
    [ObservableProperty] private int? selectedProcessor = null;
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;
    [ObservableProperty] private int matchThreashold = ConfigService.MatchProcessorThreashold;

    public SelectionModel<string> ProcessorSelection { get; } = new();

    public ProcessingPageViewModel(Slider matchThreasholdSlider)
    {
        matchThreasholdSlider.LostFocus += (object? sender, RoutedEventArgs e) => Save();
        ProcessorSelection.SelectionChanged += ProcessorSelectionChanged;

        ConfigService.OnUpdate += OnConfigUpdate;
    }

    private void ProcessorSelectionChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        SelectedProcessor = e.SelectedItems.Count != 0 ? e.SelectedIndexes[0] : null;
    }

    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedProcessor != null && SelectedProcessor < (Processors.Count - 1))
        {
            Processors.Move((int)SelectedProcessor, (int)++SelectedProcessor);
            ProcessorSelection.Select((int)SelectedProcessor);
            Save();
        }
    }

    [RelayCommand]
    private void MoveUp()
    {
        if(SelectedProcessor != null && SelectedProcessor > 0)
        {
            Processors.Move((int)SelectedProcessor, (int)--SelectedProcessor);
            ProcessorSelection.Select((int)SelectedProcessor);
            Save();
        }
    }

    private void Save()
    {
        MatchThreashold -= MatchThreashold % 10;
        ConfigService.UpdateMatchProcessors(Processors.ToList(), MatchThreashold);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        Processors = new(ConfigService.MatchProcessors);
        MatchThreashold = ConfigService.MatchProcessorThreashold;
        ConfigLocked = ConfigService.IsLocked;
    }
}

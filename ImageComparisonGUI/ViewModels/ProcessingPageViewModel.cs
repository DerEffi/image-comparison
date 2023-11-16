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
    [ObservableProperty] private AvaloniaList<string> processors = new(ConfigService.AutoProcessors);
    [ObservableProperty] private int? selectedProcessor = null;
    [ObservableProperty] private bool configLocked = ConfigService.IsLocked;
    [ObservableProperty] private int threashold = ConfigService.AutoProcessorThreashold;

    public SelectionModel<string> ProcessorSelection { get; } = new();

    public ProcessingPageViewModel(Slider ThreasholdSlider)
    {
        ThreasholdSlider.LostFocus += (object? sender, RoutedEventArgs e) => Save();
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
            int newLocation = (int)SelectedProcessor + 1;
            Processors.Move((int)SelectedProcessor, newLocation);
            ProcessorSelection.Select(newLocation);
            Save();
        }
    }

    [RelayCommand]
    private void MoveUp()
    {
        if(SelectedProcessor != null && SelectedProcessor > 0)
        {
            int newLocation = (int)SelectedProcessor - 1;
            Processors.Move((int)SelectedProcessor, newLocation);
            ProcessorSelection.Select(newLocation);
            Save();
        }
    }

    private void Save()
    {
        Threashold -= Threashold % 10;
        ConfigService.UpdateAutoProcessors(Processors.ToList(), Threashold);
    }

    public void OnConfigUpdate(object? sender, EventArgs e)
    {
        int? lastSelectedProcessor = SelectedProcessor;
        Processors = new(ConfigService.AutoProcessors);
        Threashold = ConfigService.AutoProcessorThreashold;
        ConfigLocked = ConfigService.IsLocked;
        if(lastSelectedProcessor != null)
            ProcessorSelection.Select((int)lastSelectedProcessor);
    }
}

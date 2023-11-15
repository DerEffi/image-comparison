using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ImageComparison.Services;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.ViewModels;
using System;

namespace ImageComparisonGUI.Views;

public partial class MainWindow : Window
{
    public static MainWindow Instance { get; private set; }

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        DataContext = new MainWindowViewModel(this, ClientSizeProperty);
        LogService.Log("App started");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }
}
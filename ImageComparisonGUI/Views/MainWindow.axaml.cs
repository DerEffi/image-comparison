using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.ViewModels;
using System;

namespace ImageComparisonGUI.Views;

public partial class MainWindow : Window
{
    private static MainWindow instance;
    public static MainWindow Instance { get => instance; }

    public MainWindow()
    {
        instance = this;
        InitializeComponent();
        DataContext = new MainWindowViewModel(ClientSize.Width, ClientSize.Height);
        ClientSizeProperty.Changed.Subscribe(size => Resize(size.NewValue.Value.Width, size.NewValue.Value.Height));
        Opened += (object? sender, EventArgs e) => { Resize(ClientSize.Width, ClientSize.Height); };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void Resize(double width, double height)
    {
        MainWindowViewModel vm = DataContext as MainWindowViewModel ?? new MainWindowViewModel(width, height);
        vm.Resize(width, height);
    }
}
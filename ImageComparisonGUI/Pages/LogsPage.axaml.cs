using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class LogsPage : UserControl
    {
        public LogsPage()
        {
            InitializeComponent();
            DataContext = new LogsPageViewModel();
        }
    }
}

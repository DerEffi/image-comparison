using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using ImageComparisonGUI.Services;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class AboutPage : UserControl
    {
        public AboutPage()
        {
            InitializeComponent();
            DataContext = new AboutPageViewModel();
        }
    }
}

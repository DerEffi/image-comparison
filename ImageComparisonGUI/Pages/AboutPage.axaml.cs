using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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

            this.Find<Button>("btn_author_homepage").Click += OpenAuthorHomepage;
            this.Find<Button>("btn_project_homepage").Click += OpenProjectHomepage;
            this.Find<Button>("btn_algorythm_homepage").Click += OpenAlgorythmHomepage;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OpenAuthorHomepage(object? sender, RoutedEventArgs e)
        {
            UrlService.OpenUrl("https://dereffi.de");
        }

        private void OpenProjectHomepage(object? sender, RoutedEventArgs e)
        {
            UrlService.OpenUrl("https://github.com/DerEffi/image-comparison");
        }

        private void OpenAlgorythmHomepage(object? sender, RoutedEventArgs e)
        {
            UrlService.OpenUrl("https://github.com/xnafan/Simple-image-comparison");
        }
    }
}

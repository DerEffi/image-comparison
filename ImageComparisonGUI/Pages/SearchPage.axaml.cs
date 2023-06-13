using Avalonia.Controls;
using Avalonia.Interactivity;
using ImageComparisonGUI.ViewModels;
using System;

namespace ImageComparisonGUI.Pages
{
    public partial class SearchPage : UserControl
    {
        private event EventHandler<RoutedEventArgs> leftImageDoubleTapped = delegate { };

        public SearchPage()
        {
            InitializeComponent();
            Button leftImageButton = this.Find<Button>("LeftImageButton");
            Button rightImageButton = this.Find<Button>("RightImageButton");
            DataContext = new SearchPageViewModel(leftImageButton, rightImageButton);
        }
    }
}

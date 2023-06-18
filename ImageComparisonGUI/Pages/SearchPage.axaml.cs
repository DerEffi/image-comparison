using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ImageComparisonGUI.ViewModels;
using System;

namespace ImageComparisonGUI.Pages
{
    public partial class SearchPage : UserControl
    {
        public SearchPage()
        {
            InitializeComponent();
            DataContext = new SearchPageViewModel(this);
        }
    }
}

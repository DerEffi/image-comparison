using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class LocationsPage : UserControl
    {
        public LocationsPage()
        {
            InitializeComponent();
            DataContext = new LocationsPageViewModel();
        }
    }
}

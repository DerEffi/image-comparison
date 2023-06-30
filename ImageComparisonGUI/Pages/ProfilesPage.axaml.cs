using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class ProfilesPage : UserControl
    {
        public ProfilesPage()
        {
            InitializeComponent();
            DataContext = new ProfilesPageViewModel();
        }
    }
}

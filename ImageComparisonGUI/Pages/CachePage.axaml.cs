using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class CachePage : UserControl
    {
        public CachePage()
        {
            InitializeComponent();
            DataContext = new CachePageViewModel();
        }
    }
}

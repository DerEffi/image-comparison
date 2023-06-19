using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class HotkeysPage : UserControl
    {
        public HotkeysPage()
        {
            InitializeComponent();
            DataContext = new HotkeysPageViewModel();
        }
    }
}

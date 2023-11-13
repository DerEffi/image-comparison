using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class ProcessingPage : UserControl
    {
        public ProcessingPage()
        {
            InitializeComponent();
            Slider ThreasholdSlider = this.Find<Slider>("ThreasholdSlider");
            DataContext = new ProcessingPageViewModel(ThreasholdSlider);
        }
    }
}

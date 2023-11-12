using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class ProcessingPage : UserControl
    {
        public ProcessingPage()
        {
            InitializeComponent();
            Slider matchThreasholdSlider = this.Find<Slider>("MatchThreasholdSlider");
            DataContext = new ProcessingPageViewModel(matchThreasholdSlider);
        }
    }
}

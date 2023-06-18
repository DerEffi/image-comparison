using Avalonia.Controls;
using ImageComparisonGUI.ViewModels;

namespace ImageComparisonGUI.Pages
{
    public partial class AdjustablesPage : UserControl
    {
        public AdjustablesPage()
        {
            InitializeComponent();

            Slider matchThreasholdSlider = this.Find<Slider>("MatchThreasholdSlider");
            Slider hashDetailSlider = this.Find<Slider>("HashDetailSlider");
            DataContext = new AdjustablesPageViewModel(matchThreasholdSlider, hashDetailSlider);
        }
    }
}

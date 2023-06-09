using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ImageComparisonGUI.ViewModels;

public partial class SearchPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string greeting = "Hello World!";

    public SearchPageViewModel()
    {
        this.greeting = DateTime.Now.ToString();
    }
}

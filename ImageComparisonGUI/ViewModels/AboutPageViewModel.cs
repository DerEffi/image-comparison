using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Reflection;

namespace ImageComparisonGUI.ViewModels;

public partial class AboutPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string versionText = "Hello World!";

    public AboutPageViewModel()
    {
        AssemblyName name = Assembly.GetExecutingAssembly().GetName();
        this.versionText = $"{name.Name} - Version {name.Version}";
    }
}

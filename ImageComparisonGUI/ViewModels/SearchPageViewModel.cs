using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageComparisonGUI.Models;
using ImageComparisonGUI.Services;
using System.Collections.Generic;
using System.IO;

namespace ImageComparisonGUI.ViewModels;

public partial class SearchPageViewModel : ViewModelBase
{
    #region Observables

    [ObservableProperty] private string? leftImage;
    [ObservableProperty] private string? leftImageSize;
    [ObservableProperty] private string? rightImage;
    [ObservableProperty] private string? rightImageSize;
    [ObservableProperty] private bool idle = true;
    [ObservableProperty] private bool searching = false;
    [ObservableProperty] private bool displaying = true;
    [ObservableProperty] private string statusText = "Showing Results: ";
    [ObservableProperty] private string imageCountText = "107 / 328";
    [ObservableProperty] private int percentComplete = 33;

    #endregion

    public SearchPageViewModel()
    {
        LeftImage = "D:\\Bilder\\Coding Projects\\ImageComparison\\Avatar64.jpg";
        LeftImageSize = FileService.GetReadableFilesize(LeftImage);
        RightImage = "D:\\Bilder\\Coding Projects\\ImageComparison\\Profile Transparent.png";
        RightImageSize = FileService.GetReadableFilesize(RightImage);
    }

    #region Commands

    [RelayCommand]
    private void DeleteImage(int side)
    {
        try
        {
            if (side <= 0 && LeftImage != null)
                FileService.DeleteFile(LeftImage);

            if (side >= 0 && RightImage != null)
                FileService.DeleteFile(RightImage);
        } catch { }

        NextPair();
    }

    [RelayCommand]
    private void NoMatch()
    {
        NextPair();
    }

    [RelayCommand]
    private void Search()
    {

    }

    [RelayCommand]
    private void Abort()
    {

    }

    [RelayCommand]
    private void AutoProcess()
    {
    
    }

    #endregion

    #region Data Functions

    private void NextPair()
    {

    }

    #endregion
}

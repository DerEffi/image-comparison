using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ImageComparisonGUI.Views;

namespace ImageComparisonGUI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                for (int i = BindingPlugins.DataValidators.Count - 1; i >= 0; i--)
                {
                    if (BindingPlugins.DataValidators[i] is DataAnnotationsValidationPlugin)
                    {
                        BindingPlugins.DataValidators.RemoveAt(i);
                    }
                }
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
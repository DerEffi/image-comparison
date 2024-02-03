using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using ImageComparisonGUI.Models;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ImageComparisonGUI.Services
{
    /// <summary>
    /// <para>
    /// Converts a string path to a bitmap asset.
    /// </para>
    /// <para>
    /// The asset must be in the same assembly as the program. If it isn't,
    /// specify "avares://<assemblynamehere>/" in front of the path to the asset.
    /// </para>
    /// </summary>
    public class BitmapAssetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is string path && targetType.IsAssignableFrom(typeof(Bitmap)))
            {
                try
                {
                    return new Bitmap(path);
                } catch(Exception)
                {
                    return null;
                }
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Checks for specific enum value
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : BindingOperations.DoNothing;
        }
    }

    /// <summary>
    /// Checks if enum is not a given value
    /// </summary>
    public class NotEnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts number into human readable string
    /// </summary>
    public class ReadableFilesizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value is long size)
            {
                if (size < 1000)
                {
                    return $"{size} B";
                }
                else if (size < 1000000)
                {
                    return $"{size >> 10} KB";
                }
                else if (size < 10000000)
                {
                    return $"{decimal.Divide(size, 1000000):0.00} MB";
                }
                else if (size < 100000000)
                {
                    return $"{(decimal.Divide(size, 1000000)):0.0} MB";
                }
                else
                {
                    return $"{size >> 20} MB";
                }
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts internal similarity score into human readable percantage
    /// </summary>
    public class SimilarityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return string.Format("{0:0.0}", System.Convert.ToDouble(value) / 100);

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts Saved Hotkey into human readable string
    /// </summary>
    public class DisplayHotkeyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            if(value is AvaloniaList<Hotkey> hotkeys && parameter is string needle && !string.IsNullOrEmpty(needle))
            {
                HotkeyTarget target;
                if (Enum.TryParse<HotkeyTarget>(needle, out target)) {
                    Hotkey? hotkey = hotkeys.FirstOrDefault(h => h.Target == target);
                    if (hotkey != null)
                    {
                        string modifiers = hotkey.Modifiers.ToString().Replace("None", "").Replace(", ", " + ");
                        if (!string.IsNullOrEmpty(modifiers))
                            modifiers += " + ";
                        return $"{modifiers}{hotkey.Key}";
                    }
                }
            }

            return "None";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

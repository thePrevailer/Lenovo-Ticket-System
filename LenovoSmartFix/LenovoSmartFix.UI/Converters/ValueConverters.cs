using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace LenovoSmartFix.UI.Converters;

/// <summary>int → "42 %" string</summary>
public sealed class PercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        $"{value} %";
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>bool → Visibility (true = Visible)</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>bool → Visibility (true = Collapsed — inverse)</summary>
public sealed class NotVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>bool → !bool</summary>
public sealed class NotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is not true;
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is not true;
}

/// <summary>int == 0 → Visible (show empty-state when list has no items)</summary>
public sealed class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>bool (HasUpdates) → SolidColorBrush — amber when true, green when false</summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var hasUpdates = value is true;
        var color = hasUpdates
            ? Windows.UI.Color.FromArgb(0xFF, 0xD9, 0x77, 0x06)  // amber
            : Windows.UI.Color.FromArgb(0xFF, 0x1E, 0x7E, 0x34); // success green
        return new SolidColorBrush(color);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>bool (consentRequired) → button label string</summary>
public sealed class ConsentToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? "Approve & Run" : "Already Applied";
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

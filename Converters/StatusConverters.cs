using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace NodeHiveCenter.Converters;

/// <summary>Maps node status string to a SolidColorBrush for badge backgrounds.</summary>
public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = (value as string ?? "").ToUpper();
        return status switch
        {
            "PROCESSING" => new SolidColorBrush(Color.FromRgb(0, 245, 196)),   // #00f5c4
            "DONE"       => new SolidColorBrush(Color.FromRgb(0, 180, 140)),   // dimmer accent
            _            => new SolidColorBrush(Color.FromRgb(30, 40, 51)),     // idle gray
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Maps node status string to foreground text color for badge.</summary>
public class StatusForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = (value as string ?? "").ToUpper();
        return status switch
        {
            "PROCESSING" => new SolidColorBrush(Color.FromRgb(7, 10, 15)),   // dark on accent
            "DONE"       => new SolidColorBrush(Color.FromRgb(230, 237, 243)),
            _            => new SolidColorBrush(Color.FromRgb(139, 148, 158)), // muted
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Returns true if the current phase has reached or passed the target phase.</summary>
public class IsPhaseReachedConverter : IValueConverter
{
    private static readonly List<string> Phases = new() { "received", "splitting", "processing", "aggregating", "done" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string current = (value as string ?? "").ToLower();
        string target  = (parameter as string ?? "").ToLower();
        int ci = Phases.IndexOf(current);
        int ti = Phases.IndexOf(target);
        return ci >= 0 && ti >= 0 && ci >= ti;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Returns true if this is the CURRENT (active) phase step.</summary>
public class IsCurrentPhaseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string current = (value as string ?? "").ToLower();
        string target  = (parameter as string ?? "").ToLower();
        return current == target;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Standard bool → Visibility converter.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Returns accent brush when bool is true, muted brush otherwise.</summary>
public class BoolToAccentBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromRgb(0, 245, 196))
            : new SolidColorBrush(Color.FromRgb(30, 40, 51));
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Phase reached → accent brush, else dim brush (for footer step backgrounds).</summary>
public class PhaseReachedToBrushConverter : IValueConverter
{
    private static readonly List<string> Phases = new() { "received", "splitting", "processing", "aggregating", "done" };
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string current = (value as string ?? "").ToLower();
        string target  = (parameter as string ?? "").ToLower();
        int ci = Phases.IndexOf(current);
        int ti = Phases.IndexOf(target);
        bool reached = ci >= 0 && ti >= 0 && ci >= ti;
        return reached
            ? new SolidColorBrush(Color.FromRgb(0, 245, 196))
            : new SolidColorBrush(Color.FromRgb(20, 28, 38));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Phase reached → dark foreground on accent, else muted text.</summary>
public class PhaseReachedToForegroundConverter : IValueConverter
{
    private static readonly List<string> Phases = new() { "received", "splitting", "processing", "aggregating", "done" };
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string current = (value as string ?? "").ToLower();
        string target  = (parameter as string ?? "").ToLower();
        int ci = Phases.IndexOf(current);
        int ti = Phases.IndexOf(target);
        bool reached = ci >= 0 && ti >= 0 && ci >= ti;
        return reached
            ? new SolidColorBrush(Color.FromRgb(7, 10, 15))
            : new SolidColorBrush(Color.FromRgb(100, 116, 139));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankParser.Converters;

public class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        string language
    )
    {
        if (value is double d &&
            (targetType == typeof(GridLength)))
        {
            return d switch
            {
                0 => new GridLength(64),
                double.PositiveInfinity => GridLength.Auto,
                double.NaN => new GridLength(64),
                _ => new GridLength(d),
            };
        }

        return GridLength.Auto;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        string language
    )
        => value is GridLength g && (targetType == typeof(double))
            ? g.IsAbsolute
                ? g.Value
                : g.IsStar
                    ? double.PositiveInfinity
                    : 64
            : double.NaN;
}

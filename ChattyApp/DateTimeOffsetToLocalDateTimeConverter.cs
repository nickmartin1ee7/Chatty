using System.Globalization;

namespace ChattyApp;

public class DateTimeOffsetToLocalDateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            DateTime localDateTime = dateTimeOffset.LocalDateTime;
            return localDateTime.ToString("MM/dd/yyyy h:mm tt");
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

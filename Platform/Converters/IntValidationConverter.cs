using System;
using System.Globalization;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Platform.Converters
{
    public class IntValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out int result))
                {
                    return result;
                }
                throw new FormatException("请输入有效的整数");
            }
            return 0;
        }
    }
} 
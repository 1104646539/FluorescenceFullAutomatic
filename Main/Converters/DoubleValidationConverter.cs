using System;
using System.Globalization;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Converters
{
    public class DoubleValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (double.TryParse(stringValue, out double result))
                {
                    return result;
                }
                throw new FormatException("请输入有效的数字");
            }
            return 0.0;
        }
    }
} 
using System;
using System.Globalization;
using System.Windows.Data;
using FluorescenceFullAutomatic.Model;
using static FluorescenceFullAutomatic.Model.ApplyTest;

namespace FluorescenceFullAutomatic.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ApplyTestType filterType)
            {
                switch (filterType)
                {
                    case ApplyTestType.WaitTest:
                        return "待检";
                    case ApplyTestType.TestEnd:
                        return "已检测";
                    case ApplyTestType.All:
                        return "全部";
                    default:
                        return string.Empty;
                }
            }
            else if (value == null)
            {
                return "全部";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
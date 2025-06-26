using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using NPOI.SS.UserModel;

namespace FluorescenceFullAutomatic.Converters
{
    public class StringToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string defaultValue = "--";
            if (value != null && value is string v)
            {
                if (string.IsNullOrEmpty(v))
                {
                    return defaultValue;
                }
                return v;
            }
            return defaultValue;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return value;
        }
    }
}

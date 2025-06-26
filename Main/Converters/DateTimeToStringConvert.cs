using FluorescenceFullAutomatic.Ex;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Converters
{
    public class DateTimeToStringConvert : IValueConverter
    {
        private static readonly DateTime DefaultDateTime = new DateTime(599266080000000000);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null) {
                if (value is DateTime) {
                    DateTime dateTime = (DateTime)value;
                    if (DefaultDateTime == dateTime) {
                        return "--";
                    }
                    return dateTime.GetDateTimeString();
                }
            }
            return "--";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (value is string)
                {
                    return DateTimeEx.GetDateTime((string)value);
                }
            }
            return DefaultDateTime;
        }
    }
}

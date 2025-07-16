using FluorescenceFullAutomatic.Platform.Ex;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using FluorescenceFullAutomatic.Platform.Model;

namespace FluorescenceFullAutomatic.Platform.Converters
{
    public class SampleItemResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) {
                return "-";
            }
            if (value is SampleItem)
            {
                SampleItem item = (SampleItem)value;
                if (item.ResultId <= 0 || item.TestResult == null)
                {
                    return "-";
                }
                return item.TestResult.TestVerdict;
            }
            else {
                return "未知";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}

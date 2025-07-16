using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Platform.Converters
{
    public class SampleItemStateConverter : IValueConverter
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
                    return item.State.GetDescription();
                }
                else {
                    return item.TestResult.ResultState.GetDescription();
                }
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

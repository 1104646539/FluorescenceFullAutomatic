using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Converters
{
    public class TestResultStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) {
                return "待检";
            }
            if (value is ResultState)
            {
                ResultState item = (ResultState)value;
                if (item == ResultState.TestFinish) { 
                    return "已检";
                }else if (item == ResultState.SamplingFailed || item == ResultState.ScanFailed || item == ResultState.AddSampleFailed)
                {
                    return item.GetDescription();
                }
                return "待检";
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

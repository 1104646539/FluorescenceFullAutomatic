using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using FluorescenceFullAutomatic.Ex;
using FluorescenceFullAutomatic.Model;

namespace FluorescenceFullAutomatic.Converters
{
    public class ResultToImageConverter : IValueConverter
    {
        string path = "../Image/";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResultState v)
            {
                ResultState item = v;
                if (item == ResultState.TestFinish)
                {
                    return path + "img_result_state_finish.png";
                }
                else if (
                    item == ResultState.SamplingFailed
                    || item == ResultState.ScanFailed
                    || item == ResultState.AddSampleFailed
                )
                {
                    return path + "img_result_state_error.png";
                }
                return path + "img_result_state_none.png";
            }

            return path + "img_result_state_none.png";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Utils;

namespace FluorescenceFullAutomatic.Platform.Converters
{
    public class ResultToImageConverter : IValueConverter
    {
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ResultState v)
            {
                ResultState item = v;
                if (item == ResultState.TestFinish)
                {
                    return GlobalUtil.Platform_Img_Path + "img_result_state_finish.png";
                }
                else if (
                    item == ResultState.SamplingFailed
                    || item == ResultState.ScanFailed
                    || item == ResultState.AddSampleFailed
                )
                {
                    return GlobalUtil.Platform_Img_Path + "img_result_state_error.png";
                }
                return GlobalUtil.Platform_Img_Path + "img_result_state_none.png";
            }

            return GlobalUtil.Platform_Img_Path + "img_result_state_none.png";
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

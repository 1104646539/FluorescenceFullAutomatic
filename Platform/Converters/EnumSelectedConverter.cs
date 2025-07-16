using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Platform.Converters
{
    public class EnumSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool selected =  value != null && value.Equals(parameter) ;
            return selected;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return parameter; // 如果选中，返回参数值
            }
            return Binding.DoNothing; // 如果未选中，返回null

        }
    }
}

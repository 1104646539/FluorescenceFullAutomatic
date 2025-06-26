using System;
using System.Globalization;
using System.Windows.Data;

namespace FluorescenceFullAutomatic.Converters
{
    public class SafePatientPropertyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string defaultValue = "--";
            if (value == null)
            {
                return defaultValue;
            }

            if (parameter == null)
            {
                return defaultValue;
            }

            string propertyName = parameter.ToString();
            try
            {
                var obj = value as dynamic;
                var propertyValue = obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
                
                if (propertyValue == null || (propertyValue is string str && string.IsNullOrEmpty(str)))
                {
                    return defaultValue;
                }
                
                return propertyValue.ToString();
            }
            catch
            {
                return defaultValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FluorescenceFullAutomatic.Platform.Model;

namespace FluorescenceFullAutomatic.Platform.Converters
{
    public class ReactionAreaProjectTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            if (value is ReactionAreaItem item)
            {
                if (value == null || item.TestResult == null || item.TestResult.Project == null)
                    return Visibility.Collapsed;

                return item.TestResult.Project.ProjectType == Project.Project_Type_Double ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
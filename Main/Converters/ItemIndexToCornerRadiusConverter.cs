using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;

namespace FluorescenceFullAutomatic.Converters
{
    public class ItemIndexToCornerRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: ListBoxItem
            // values[1]: ListBox
            var item = values[0] as DependencyObject;
            var itemsControl = values[1] as ItemsControl;
            if (item == null || itemsControl == null)
                return new CornerRadius(0);

            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(item);
            int count = itemsControl.Items.Count;

            if (index == 0)
                return new CornerRadius(10, 0, 0, 10);
            else if (index == count - 1)
                return new CornerRadius(0, 10, 10, 0);
            else
                return new CornerRadius(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
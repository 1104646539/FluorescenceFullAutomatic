using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FluorescenceFullAutomatic.Views.Ctr
{
    public class ButtonPopupHelper
    {
        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.RegisterAttached("IsPopupOpen", typeof(bool), typeof(ButtonPopupHelper), 
                new PropertyMetadata(false, OnIsPopupOpenChanged));

        public static readonly DependencyProperty MenuItemsProperty =
            DependencyProperty.RegisterAttached("MenuItems", typeof(IEnumerable), typeof(ButtonPopupHelper), 
                new PropertyMetadata(null, OnMenuItemsChanged));

        private static readonly DependencyProperty PopupProperty =
            DependencyProperty.RegisterAttached("Popup", typeof(Popup), typeof(ButtonPopupHelper), 
                new PropertyMetadata(null));

        public static bool GetIsPopupOpen(Button button)
        {
            return (bool)button.GetValue(IsPopupOpenProperty);
        }

        public static void SetIsPopupOpen(Button button, bool value)
        {
            button.SetValue(IsPopupOpenProperty, value);
        }

        public static IEnumerable GetMenuItems(Button button)
        {
            return (IEnumerable)button.GetValue(MenuItemsProperty);
        }

        public static void SetMenuItems(Button button, IEnumerable value)
        {
            button.SetValue(MenuItemsProperty, value);
        }

        private static Popup GetPopup(Button button)
        {
            return (Popup)button.GetValue(PopupProperty);
        }

        private static void SetPopup(Button button, Popup value)
        {
            button.SetValue(PopupProperty, value);
        }

        private static void OnIsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                var popup = GetPopup(button);
                if (popup != null)
                {
                    popup.IsOpen = (bool)e.NewValue;
                }
            }
        }

        private static void OnMenuItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                var popup = GetPopup(button);
                if (popup == null)
                {
                    popup = new Popup
                    {
                        Placement = PlacementMode.Bottom,
                        PlacementTarget = button,
                        StaysOpen = false
                    };

                    popup.Closed += (s, args) => {
                        SetIsPopupOpen(button, false);
                    };

                    var border = new Border
                    {
                        Background = System.Windows.Media.Brushes.White,
                        BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DDDDDD")),
                        BorderThickness = new Thickness(1)
                    };

                    var itemsControl = new ItemsControl
                    {
                        ItemsSource = e.NewValue as IEnumerable
                    };

                    itemsControl.ItemTemplate = new DataTemplate();
                    var factory = new FrameworkElementFactory(typeof(MenuItem));
                    factory.SetBinding(MenuItem.HeaderProperty, new System.Windows.Data.Binding("Header"));
                    factory.SetBinding(MenuItem.CommandProperty, new System.Windows.Data.Binding("Command"));
                    itemsControl.ItemTemplate.VisualTree = factory;

                    border.Child = itemsControl;
                    popup.Child = border;

                    SetPopup(button, popup);
                }
                else
                {
                    if (popup.Child is Border border && border.Child is ItemsControl itemsControl)
                    {
                        itemsControl.ItemsSource = e.NewValue as IEnumerable;
                    }
                }
            }
        }
    }
} 
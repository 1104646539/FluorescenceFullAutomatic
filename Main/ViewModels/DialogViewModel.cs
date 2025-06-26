using CommunityToolkit.Mvvm.ComponentModel;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class DialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private object vm;

        [ObservableProperty]
        private UserControl page;

        [ObservableProperty]
        private WindowState windowState = WindowState.Normal;

        [ObservableProperty]
        private ResizeMode resizeMode = ResizeMode.NoResize;

        [ObservableProperty]
        private bool showCloseButton = false;

        [ObservableProperty]
        private SizeToContent sizeToContent = SizeToContent.WidthAndHeight;

        [ObservableProperty]
        private double minHeight = 200;

        [ObservableProperty]
        private double minWidth = 600;

        [ObservableProperty]
        private string title = string.Empty;

        public DialogViewModel()
        {
        }

        public DialogViewModel(WindowState windowState = WindowState.Normal,
            ResizeMode resizeMode = ResizeMode.NoResize,
            bool showCloseButton = false,
            SizeToContent sizeToContent = SizeToContent.WidthAndHeight,
            double minHeight = 200,
            double minWidth = 600,
            string title = "")
        {
            WindowState = windowState;
            ResizeMode = resizeMode;
            ShowCloseButton = showCloseButton;
            SizeToContent = sizeToContent;
            MinHeight = minHeight;
            MinWidth = minWidth;
            Title = title;
        }
    }
}

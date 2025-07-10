using System;
using System.Windows;
using FluorescenceFullAutomatic.Config;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.Views.Ctr;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using Serilog;

namespace FluorescenceFullAutomatic.Views
{
    /// <summary>
    /// MainWindow.xaml µÄ½»»¥Âß¼­
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public static MetroWindow Instance;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
            //var settings = new MetroDialogSettings()
            //{
            //    AffirmativeButtonText = "OK",
            //    NegativeButtonText = "Go away!",
            //    FirstAuxiliaryButtonText = "Cancel",
            //    MaximumBodyHeight = 100,
            //    ColorScheme = MetroDialogColorScheme.Accented,
            //};
            //this.MetroDialogOptions = settings;

        }

    }
}

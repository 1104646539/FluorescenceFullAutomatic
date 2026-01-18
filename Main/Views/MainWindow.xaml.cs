using System;
using System.Windows;
using System.Windows.Interop;
using FluorescenceFullAutomatic.Core.Config;
using FluorescenceFullAutomatic.Views.Ctr;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using Serilog;

namespace FluorescenceFullAutomatic.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
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
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwndSource = (HwndSource)PresentationSource.FromVisual(this);
            if (hwndSource != null)
            { 
                hwndSource.AddHook(WindowProc);
            }
        }

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xF010;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 2;
        private const int WM_TOUCH = 0x0240;
        private const int WM_GESTURE = 0x0119;

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Log.Information("WindowProc msg: {msg}, wParam: {wParam}, lParam: {lParam}，{hwnd}，{WM_SYSCOMMAND},{SC_MOVE},{WM_NCLBUTTONDOWN},{HTCAPTION},{WM_TOUCH},{WM_GESTURE}"
            //    , msg, wParam, lParam, hwnd, WM_SYSCOMMAND, SC_MOVE, WM_NCLBUTTONDOWN, HTCAPTION, WM_TOUCH, WM_GESTURE);

            if (msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MOVE)
            {
                // 禁止所有移动窗口的操作
                handled = true;
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }
    }
}

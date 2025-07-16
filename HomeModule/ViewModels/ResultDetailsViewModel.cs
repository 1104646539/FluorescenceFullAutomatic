using System;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Platform.Core.Config;
using FluorescenceFullAutomatic.Platform.Model;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace FluorescenceFullAutomatic.HomeModule.ViewModels
{
    public partial class ResultDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        TestResult result;

        [ObservableProperty]
        Action closeAction;
        [ObservableProperty]
        Action<TestResult> confirmAction;

        [ObservableProperty]
        bool isDebugMode;

        [ObservableProperty]
        bool isDualCard;

        [ObservableProperty]
        private bool isDetailsVisible;

        public string DetailsButtonText => IsDetailsVisible ? "收起" : "更多信息";

        private WpfPlot plotView;


        public ResultDetailsViewModel()
        {
            IsDebugMode = GlobalConfig.Instance.IsDebug;
            IsDetailsVisible = true;  // 初始状态
        }
      
        public void SetPlotView(WpfPlot plotView)
        {
            this.plotView = plotView;
            UpdateChartData();
        }
        [RelayCommand]
        public void ConfirmSave() {
            Result.Patient = Patient;
            ConfirmAction?.Invoke(Result);
        }
        [RelayCommand]
        public void ClickClose()
        {
            CloseAction?.Invoke();
        }

        [RelayCommand]
        private void ToggleDetails()
        {
            IsDetailsVisible = !IsDetailsVisible;
            OnPropertyChanged(nameof(DetailsButtonText));
        }
        [ObservableProperty]
        Patient patient;
        private void UpdateChartData()
        {
            if (Result?.Patient == null)
            {
                Patient = new Patient();
                Patient.InspectDate = DateTime.Now;
            }
            else {
                Patient = Result.Patient;
            }
            if (Result?.Point?.Points == null)
            {
                return;
            }
            if (Result?.Project != null)
            {
                IsDualCard = Result.Project?.ProjectType == Project.Project_Type_Double;
            }
            string scanStartStr = Result?.Project?.ScanStart;
            int.TryParse(scanStartStr, out int scanStart);
            int[] pos = Result?.Point?.Points;
            this.plotView.Plot.Clear();
            Scatter myScatter = null;
            int[] xs = new int[pos.Length];
            for (int i = 0; i < pos.Length; i++)
            {
                xs[i] = i + scanStart;
            }
            myScatter = this.plotView.Plot.Add.Scatter(xs, pos);

            //myScatter.Color = ScottPlot.Colors.Green;
            myScatter.LineWidth = 5;
            myScatter.MarkerSize = 15;
            myScatter.MarkerShape = MarkerShape.None;
            myScatter.LinePattern = LinePattern.Solid;

            this.plotView.Refresh();
        }
    }
}

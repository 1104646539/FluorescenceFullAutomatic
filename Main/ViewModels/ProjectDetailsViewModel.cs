using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class ProjectDetailsViewModel : ObservableObject
    {
        private readonly IProjectService projectRepository;
        private Project currentProject;

        [ObservableProperty]
        private bool isDoubleCard;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string identifierCode;

        [ObservableProperty]
        private string batchNum;

        [ObservableProperty]
        private string projectCode;

        [ObservableProperty]
        private string projectUnit;

        [ObservableProperty]
        private double peakWidth;

        [ObservableProperty]
        private double peakDistance;

        [ObservableProperty]
        private double scanStart;

        [ObservableProperty]
        private double scanEnd;

        [ObservableProperty]
        private int projectLjz;

        [ObservableProperty]
        private double a1;

        [ObservableProperty]
        private double a2;

        [ObservableProperty]
        private double x0;

        [ObservableProperty]
        private double p;

        [ObservableProperty]
        private string projectUnit2;

        [ObservableProperty]
        private int projectLjz2;

        [ObservableProperty]
        private int conMax;

        [ObservableProperty]
        private double a12;

        [ObservableProperty]
        private double a22;

        [ObservableProperty]
        private double x02;

        [ObservableProperty]
        private double p2;

        [ObservableProperty]
        private int conMax2;

        public Action<bool> CloseAction;
        public Action<bool> SaveAction;
        public ProjectDetailsViewModel(IProjectService projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public void LoadProject(int projectId)
        {
            currentProject = projectRepository.GetProjectForID(projectId);
            if (currentProject != null)
            {
                IsDoubleCard = currentProject.ProjectType == 1;
                Name = currentProject.ProjectName;
                IdentifierCode = currentProject.IdentifierCode;
                BatchNum = currentProject.BatchNum;
                ProjectCode = currentProject.ProjectCode;
                ProjectUnit = currentProject.ProjectUnit;
                PeakWidth = double.Parse(currentProject.PeakWidth);
                PeakDistance = double.Parse(currentProject.PeakDistance);
                ScanStart = double.Parse(currentProject.ScanStart);
                ScanEnd = double.Parse(currentProject.ScanEnd);
                ProjectLjz = currentProject.ProjectLjz;
                A1 = currentProject.A1;
                A2 = currentProject.A2;
                X0 = currentProject.X0;
                P = currentProject.P;
                ProjectUnit2 = currentProject.ProjectUnit2;
                ProjectLjz2 = currentProject.ProjectLjz2;
                ConMax = currentProject.ConMax;
                A12 = currentProject.A12;
                A22 = currentProject.A22;
                X02 = currentProject.X02;
                P2 = currentProject.P2;
                ConMax2 = currentProject.ConMax2;
            }
        }
        [RelayCommand]
        private void Close() {
            CloseAction?.Invoke(true);
        }
        [RelayCommand]
        private void Save()
        {
            try
            {
                if (currentProject == null) return;

                // 更新项目属性
                currentProject.ProjectName = Name;
                currentProject.ProjectCode = ProjectCode;
                currentProject.ProjectUnit = ProjectUnit;
                currentProject.PeakWidth = PeakWidth.ToString();
                currentProject.PeakDistance = PeakDistance.ToString();
                currentProject.ScanStart = ScanStart.ToString();
                currentProject.ScanEnd = ScanEnd.ToString();
                currentProject.ProjectLjz = ProjectLjz;
                currentProject.A1 = A1;
                currentProject.A2 = A2;
                currentProject.X0 = X0;
                currentProject.P = P;
                currentProject.ProjectUnit2 = ProjectUnit2;
                currentProject.ProjectLjz2 = ProjectLjz2;
                currentProject.ConMax = ConMax;
                currentProject.A12 = A12;
                currentProject.A22 = A22;
                currentProject.X02 = X02;
                currentProject.P2 = P2;
                currentProject.ConMax2 = ConMax2;

                // 保存到数据库
                bool ret = projectRepository.UpdateProject(currentProject);
                if (ret)
                {
                    MessageBox.Show("保存成功！", "提示");
                    SaveAction?.Invoke(true);
                }
                else
                {
                    MessageBox.Show($"保存失败");
                    SaveAction?.Invoke(false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误");
            }
        }
    }
}

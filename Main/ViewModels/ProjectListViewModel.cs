using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.Views.Ctr;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.ViewModels
{
    /// <summary>
    /// 项目列表
    /// </summary>
    public partial class ProjectListViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<Project> projects;
        [ObservableProperty]
        private bool isDefault;
        private IProjectService projectRepository;
        [ObservableProperty]
        private string title;
        [ObservableProperty]
        private bool isLoading = false;
        [ObservableProperty]
        private string btnContent;
        public ProjectListViewModel(IProjectService projectRepository)
        {
            this.projectRepository = projectRepository;
            projects = new ObservableCollection<Project>();
            SetTitle(false);
        }
        public void SetTitle(bool isDefault){
            IsDefault = isDefault;
            Title = isDefault ? "默认项目列表" : "项目列表";
            BtnContent = IsDefault ? "切换项目列表" : "切换默认项目列表";
        }
        [RelayCommand]
        public void ChangeProjectDefault() {
            SetTitle(!IsDefault);
            Loaded();
        }
        [RelayCommand]
        private void Edit(Project project) {
            var dialog = new DialogWindow();
            var projectDetailsViewModel = new ProjectDetailsViewModel(projectRepository);
            projectDetailsViewModel.LoadProject(project.Id);
            projectDetailsViewModel.CloseAction = (bool ret) => {
                    dialog.Close();
            };
            projectDetailsViewModel.SaveAction = (bool ret) => {
                if(ret) {
                    dialog.Close();
                }
            };
            var projectDetailsPage = new ProjectDetailsControl();
            projectDetailsPage.DataContext = projectDetailsViewModel;
            dialog.DataContext = new DialogViewModel(showCloseButton:true){
                Page = projectDetailsPage
            };
            dialog.ShowDialog();
        }
        [RelayCommand]
        private void Loaded()
        {
            IsLoading = true;
            LoadProject();
        }
        private async void LoadProject(){
            var projects = await projectRepository.GetAllProjectAsync(IsDefault);
            Projects = new ObservableCollection<Project>(projects);
            IsLoading = false;
        }

    }
}

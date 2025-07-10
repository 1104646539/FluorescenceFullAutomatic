using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Services;
using FluorescenceFullAutomatic.Utils;
using FluorescenceFullAutomatic.Views;
using FluorescenceFullAutomatic.Views.Ctr;
using MahApps.Metro.Controls.Dialogs;
using Serilog;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class ApplyTestViewModel : ObservableObject
    {
        private readonly IApplyTestService applyTestRepository;
        private readonly IPatientService patientRepository;
        private readonly ILisService lisRepository;
        private readonly IDialogService dialogRepository;


        // 状态过滤选项
        [ObservableProperty]
        private ObservableCollection<ApplyTestType> filterTypes =
            new ObservableCollection<ApplyTestType>
            {
                ApplyTestType.WaitTest,
                ApplyTestType.TestEnd,
                ApplyTestType.All,
            };

        [ObservableProperty]
        private ApplyTestType selectedFilterType = ApplyTestType.WaitTest;

        // 列表数据
        [ObservableProperty]
        private ObservableCollection<ApplyTest> applyTestList =
            new ObservableCollection<ApplyTest>();

        // 获取选中的数据列表
        public IEnumerable<ApplyTest> SelectedTests => ApplyTestList.Where(x => x.IsSelected);

        // 当前选中项
        [ObservableProperty]
        private ApplyTest selectedApplyTest;

        // 是否为添加模式
        [ObservableProperty]
        private bool isAddMode;

        // 右侧详情数据源
        [ObservableProperty]
        private ApplyTest editTest;

        // 保存按钮可用性
        public bool CanSave => (IsAddMode && EditTest != null) || (!IsAddMode && EditTest != null);

        // 删除按钮可用性
        public bool CanDelete => !IsAddMode && EditTest != null;

        public ApplyTestViewModel(IApplyTestService applyTestRepository, IPatientService patientRepository,
            ILisService lisRepository, IDialogService dialogRepository)
        {
            this.applyTestRepository = applyTestRepository;
            this.patientRepository = patientRepository;
            this.lisRepository = lisRepository;
            this.dialogRepository = dialogRepository;
            // 默认加载待检
            LoadApplyTestList(ApplyTestType.WaitTest);
            ChangeAddMode();
            RegisterMsg();
        }

        private void RegisterMsg()
        {
            WeakReferenceMessenger.Default.Register<EventMsg<DataChangeMsg>>(
                this,
                (r, m) =>
                {
                    if (m.What == EventWhat.WHAT_CHANGE_APPLY_TEST)
                    {
                        Log.Information($"data 更新={m.Value.ID}");
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            RefreshChange(m.Value.ID);
                        });
                    }
                }
            );
        }

        /// <summary>
        /// 刷新状态
        /// </summary>
        /// <param name="id"></param>
        private void RefreshChange(int id)
        {
            ApplyTest applyTest = applyTestRepository.GetApplyTestForID(id);
            if (applyTest != null)
            {
                int index = -1;
                for (int i = 0; i < ApplyTestList.Count; i++)
                {
                    if (ApplyTestList[i].Id == id)
                    {
                        index = i;
                        break;
                    }
                }
                if (index < 0)
                {
                    return;
                }
                if (SelectedFilterType == ApplyTestType.All)
                {
                    ApplyTestList[index] = applyTest;
                }
                else if (SelectedFilterType == ApplyTestType.WaitTest)
                {
                    ApplyTestList.Remove(ApplyTestList[index]);
                }
                else
                {
                    ApplyTestList.Insert(0, applyTest);
                }
            }
        }

        partial void OnSelectedFilterTypeChanged(ApplyTestType value)
        {
            LoadApplyTestList(value);
        }

        // 加载列表
        private async void LoadApplyTestList(ApplyTestType type)
        {
            var list = await applyTestRepository.GetApplyTests(type);

            // 确保所有项的选中状态为false
            foreach (var item in list)
            {
                item.IsSelected = false;
            }

            ApplyTestList = new ObservableCollection<ApplyTest>(list);
            SelectedApplyTest = null;
            OnPropertyChanged(nameof(SelectedTests));
        }

        // 新增
        [RelayCommand]
        private void Add()
        {
            ChangeAddMode();
        }

        [RelayCommand]
        private void GetApplyTest()
        {
            CustomDialog customDialog = new CustomDialog();

            // 创建自定义的对话框
            var content = new GetApplyTestDialog();

            // 创建对话框ViewModel
            var viewModel = new GetApplyTestDialogViewModel(
                lisRepository,
                async (vm, applyTests) =>
                {
                    // 确认按钮点击后，将获取到的ApplyTest添加到系统中
                    if (applyTests != null && applyTests.Count > 0)
                    {
                        // 添加所有获取到的ApplyTest
                        foreach (var test in applyTests)
                        {
                            // 保存Patient信息
                            int patientId = patientRepository.InsertPatient(test.Patient);

                            // 创建新的ApplyTest并设置为待检状态
                            var newApplyTest = new ApplyTest
                            {
                                Barcode = test.Barcode,
                                TestNum = test.TestNum,
                                ApplyTestType = ApplyTestType.WaitTest,
                                PatientId = patientId,
                                Patient = test.Patient,
                            };

                            // 插入ApplyTest记录
                            applyTestRepository.InsertApplyTest(newApplyTest);
                        }

                        // 重新加载列表
                        LoadApplyTestList(SelectedFilterType);
                        // 关闭对话框
                        await MainWindow.Instance.HideMetroDialogAsync(customDialog);

                        //显示提示
                        dialogRepository.ShowHiltDialog(
                            "提示",
                            $"成功添加 {applyTests.Count} 条记录",
                            "确定",
                            (d, dialog) =>
                            {
                                Log.Information($"成功添加 {applyTests.Count} 条记录");
                            }
                        );
                    }
                },
                vm =>
                {
                    // 取消按钮点击后，关闭对话框
                    MainWindow.Instance.HideMetroDialogAsync(customDialog);
                }
            );

            // 设置DataContext
            content.DataContext = viewModel;
            customDialog.Content = content;
            MainWindow.Instance.ShowMetroDialogAsync(customDialog);
        }

        /// <summary>
        /// 删除
        /// </summary>
        [RelayCommand]
        private void DeleteSelected()
        {
            foreach (var item in SelectedTests)
            {
                applyTestRepository.DeleteApplyTest(item);
            }
            LoadApplyTestList(SelectedFilterType);
        }

        // 新增测试数据
        [RelayCommand]
        private void TestAdd()
        {
            // 测试数据
            string[] barcodes = { "aabbcc1", "aabbcc2", "aabbcc3", "aabbcc4" };
            for (int i = 0; i < 4; i++)
            {
                Patient patient = new Patient
                {
                    PatientName = "张三" + i,
                    PatientGender = "男",
                    PatientAge = 30 + i + "",
                    InspectDate = DateTime.Now,
                    InspectDepartment = "内科",
                    InspectDoctor = "李四",
                    TestDoctor = "王五",
                    CheckDoctor = "赵六",
                };
                int patientId = patientRepository.InsertPatient(patient);
                var test = new ApplyTest
                {
                    Barcode = barcodes[i % barcodes.Length],
                    TestNum = i + 1 + "",
                    ApplyTestType = ApplyTestType.WaitTest,
                    PatientId = patientId,
                };
                applyTestRepository.InsertApplyTest(test);
            }
            LoadApplyTestList(SelectedFilterType);
        }

        /// <summary>
        /// 切换到新增模式
        /// </summary>
        private void ChangeAddMode()
        {
            IsAddMode = true;
            EditTest = new ApplyTest { Patient = new Patient() { InspectDate = DateTime.Now } };
            SelectedApplyTest = null;
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDelete));
        }

        // 保存命令
        [RelayCommand]
        private async Task Save()
        {
            if (EditTest == null)
                return;
            if (IsAddMode)
            {
                // 新增
                await Task.Run(() =>
                {
                    int patientId = patientRepository.InsertPatient(EditTest.Patient);
                    EditTest.PatientId = patientId;
                    int applyTestId = applyTestRepository.InsertApplyTest(EditTest);
                    LoadApplyTestList(SelectedFilterType);
                });
            }
            else
            {
                // 编辑
                await Task.Run(() =>
                {
                    patientRepository.UpdatePatient(EditTest.Patient);
                    applyTestRepository.UpdateApplyTest(EditTest);
                    LoadApplyTestList(SelectedFilterType);
                });
            }
            //IsAddMode = false;
            EditTest = new ApplyTest { Patient = new Patient() };
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDelete));
        }

        // 删除命令
        [RelayCommand]
        private async Task DeleteSingle()
        {
            if (EditTest == null || IsAddMode)
                return;
            // 删除
            patientRepository.DeletePatient(EditTest.Patient);
            applyTestRepository.DeleteApplyTest(EditTest);
            // 刷新
            LoadApplyTestList(SelectedFilterType);
            EditTest = null;
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDelete));
        }

        /// <summary>
        /// 选中列表项时，更新右侧详情数据，变更为编辑模式
        /// </summary>
        /// <param name="value"></param>
        partial void OnSelectedApplyTestChanged(ApplyTest value)
        {
            if (value != null)
            {
                IsAddMode = false;
                // 深拷贝，避免直接修改列表数据
                EditTest = new ApplyTest
                {
                    Id = value.Id,
                    Barcode = value.Barcode,
                    TestNum = value.TestNum,
                    ApplyTestType = value.ApplyTestType,
                    PatientId = value.PatientId,
                    Patient =
                        value.Patient != null
                            ? new Patient
                            {
                                Id = value.Patient.Id,
                                PatientName = value.Patient.PatientName,
                                PatientGender = value.Patient.PatientGender,
                                PatientAge = value.Patient.PatientAge,
                                InspectDate = value.Patient.InspectDate,
                                InspectDepartment = value.Patient.InspectDepartment,
                                InspectDoctor = value.Patient.InspectDoctor,
                                TestDoctor = value.Patient.TestDoctor,
                                CheckDoctor = value.Patient.CheckDoctor,
                            }
                            : new Patient(),
                };
            }
            else
            {
                if (!IsAddMode)
                    EditTest = null;
            }
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanDelete));
        }

        // 处理选中状态变化
        partial void OnApplyTestListChanged(ObservableCollection<ApplyTest> value)
        {
            if (value != null)
            {
                foreach (var item in value)
                {
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ApplyTest.IsSelected))
                        {
                            OnPropertyChanged(nameof(SelectedTests));
                        }
                    };
                }
            }
        }
    }
}

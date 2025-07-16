using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Platform.Ex;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;
using FluorescenceFullAutomatic.UploadModule.Upload;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class GetApplyTestDialogViewModel : ObservableRecipient
    {
        private readonly ILisService lisRepository;

        // 标题和消息
        [ObservableProperty]
        private string title = "获取申请检测";

        // 查询类型控制
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool isTestNumQueryMode = true;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool isBarcodeQueryMode;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool isApplyTimeQueryMode;

        // 查询文本
        [ObservableProperty]
        private string queryText = "";

        // 终止编号查询文本
        [ObservableProperty]
        private string endQueryText = "";

        // 送检时间查询
        [ObservableProperty]
        private DateTime? startApplyTime;

        [ObservableProperty]
        private DateTime? endApplyTime;

        // 水印文本
        [ObservableProperty]
        private string startWatermarkText = "请输入检测编号起始";

        [ObservableProperty]
        private string endWatermarkText = "请输入检测编号终止";

        // 终止编号输入框可见性
        [ObservableProperty]
        private Visibility endVisibility = Visibility.Visible;

        // 时间选择器可见性
        [ObservableProperty]
        private Visibility timePickerVisibility = Visibility.Collapsed;

        // 文本框可见性
        [ObservableProperty]
        private Visibility textBoxVisibility = Visibility.Visible;

        // 查询状态
        [ObservableProperty]
        private string statusMessage = "请输入查询条件并点击查询按钮";

        [ObservableProperty]
        private SolidColorBrush statusColor = new SolidColorBrush(Colors.Black);

        // 结果列表
        [ObservableProperty]
        private ObservableCollection<ApplyTest> applyTestList = new ObservableCollection<ApplyTest>();

        [ObservableProperty]
        private Visibility resultVisibility = Visibility.Collapsed;

        // 对话框按钮控制
        [ObservableProperty]
        private string confirmText = "确认选择";

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private string cancelText = "取消";

        [NotifyPropertyChangedRecipients]
        [ObservableProperty]
        private string closeText = "";

        [ObservableProperty]
        private Visibility showCancel;

        [ObservableProperty]
        private Visibility showClose;

        [ObservableProperty]
        private bool canConfirm = false;

        // 操作回调
        private readonly Action<GetApplyTestDialogViewModel, List<ApplyTest>> _actionConfirm;
        private readonly Action<GetApplyTestDialogViewModel> _actionCancel;
        private readonly Action<GetApplyTestDialogViewModel> _actionClose;

        public GetApplyTestDialogViewModel(
            ILisService lisRepository,
            Action<GetApplyTestDialogViewModel, List<ApplyTest>> actionConfirm,
            Action<GetApplyTestDialogViewModel> actionCancel = null,
            Action<GetApplyTestDialogViewModel> actionClose = null)
        {
            this.lisRepository = lisRepository;
            _actionConfirm = actionConfirm;
            _actionCancel = actionCancel;
            _actionClose = actionClose;

            ShowCancel = string.IsNullOrEmpty(CancelText) ? Visibility.Collapsed : Visibility.Visible;
            ShowClose = string.IsNullOrEmpty(CloseText) ? Visibility.Collapsed : Visibility.Visible;
        }

        // 属性变化处理
        protected override void Broadcast<T>(T oldValue, T newValue, string? propertyName)
        {
            base.Broadcast(oldValue, newValue, propertyName);

            if (propertyName == nameof(CancelText))
            {
                ShowCancel = string.IsNullOrEmpty(CancelText) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (propertyName == nameof(CloseText))
            {
                ShowClose = string.IsNullOrEmpty(CloseText) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (propertyName == nameof(IsTestNumQueryMode))
            {
                if (IsTestNumQueryMode)
                {
                    StartWatermarkText = "请输入检测编号起始";
                    EndWatermarkText = "请输入检测编号终止";
                    EndVisibility = Visibility.Visible;
                    TextBoxVisibility = Visibility.Visible;
                    TimePickerVisibility = Visibility.Collapsed;
                    IsBarcodeQueryMode = false;
                    IsApplyTimeQueryMode = false;
                }
            }
            else if (propertyName == nameof(IsBarcodeQueryMode))
            {
                if (IsBarcodeQueryMode)
                {
                    StartWatermarkText = "请输入条码";
                    EndVisibility = Visibility.Collapsed;
                    TextBoxVisibility = Visibility.Visible;
                    TimePickerVisibility = Visibility.Collapsed;
                    IsTestNumQueryMode = false;
                    IsApplyTimeQueryMode = false;
                }
            }
            else if (propertyName == nameof(IsApplyTimeQueryMode))
            {
                if (IsApplyTimeQueryMode)
                {
                    TextBoxVisibility = Visibility.Collapsed;
                    TimePickerVisibility = Visibility.Visible;
                    EndVisibility = Visibility.Visible;
                    IsTestNumQueryMode = false;
                    IsBarcodeQueryMode = false;
                }
            }
            else if (propertyName == nameof(EndApplyTime))
            {
                if (EndApplyTime.HasValue && StartApplyTime.HasValue && EndApplyTime.Value < StartApplyTime.Value)
                {
                    StatusMessage = "终止时间不能小于起始时间";
                    StatusColor = new SolidColorBrush(Colors.Red);
                }
            }
        }

        // 查询命令
        [RelayCommand]
        private async Task Query()
        {
            if (IsApplyTimeQueryMode)
            {
                if (!StartApplyTime.HasValue)
                {
                    StatusMessage = "请选择起始时间";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    return;
                }
                if (!EndApplyTime.HasValue)
                {
                    StatusMessage = "请选择终止时间";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    return;
                }
                if (EndApplyTime.Value < StartApplyTime.Value)
                {
                    StatusMessage = "终止时间不能小于起始时间";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    return;
                }
            }
            else if (string.IsNullOrWhiteSpace(QueryText))
            {
                StatusMessage = "请输入查询条件";
                StatusColor = new SolidColorBrush(Colors.Red);
                return;
            }

            StatusMessage = "正在查询...";
            StatusColor = new SolidColorBrush(Colors.Black);
            ResultVisibility = Visibility.Collapsed;
            CanConfirm = false;

            try
            {
                Hl7Result.QueryResult result;

                if (IsTestNumQueryMode)
                {
                    // 使用condition1和condition2
                    string condition2 = string.IsNullOrWhiteSpace(EndQueryText) ? QueryText : EndQueryText;
                    result = await lisRepository.QueryApplyTestFormTestNumAsync(QueryText, condition2);
                }
                else if (IsBarcodeQueryMode)
                {
                    // 条码查询只使用condition1
                    result = await lisRepository.QueryApplyTestFormBarcodeAsync(QueryText);
                }
                else
                {
                    // 送检时间查询
                    result = await lisRepository.QueryApplyTestFormInspectDateAsync(StartApplyTime?.ToString(DateTimeEx.DateTimeFormat3), EndApplyTime?.ToString(DateTimeEx.DateTimeFormat3));
                }

                // 处理查询结果
                HandleQueryResult(result);
            }
            catch (Exception ex)
            {
                StatusMessage = $"查询出错: {ex.Message}";
                StatusColor = new SolidColorBrush(Colors.Red);
            }
        }

        // 处理查询结果
        private void HandleQueryResult(Hl7Result.QueryResult result)
        {
            switch (result.ResultType)
            {
                case Hl7Result.QueryResultType.Success:
                    if (result.ApplyTests != null && result.ApplyTests.Count > 0)
                    {
                        ApplyTestList = new ObservableCollection<ApplyTest>(result.ApplyTests);
                        StatusMessage = $"查询成功，共找到 {result.ApplyTests.Count} 条记录";
                        StatusColor = new SolidColorBrush(Colors.Green);
                        ResultVisibility = Visibility.Visible;
                        CanConfirm = true;
                    }
                    else
                    {
                        StatusMessage = "查询成功，但未找到记录";
                        StatusColor = new SolidColorBrush(Colors.Orange);
                        ResultVisibility = Visibility.Collapsed;
                        CanConfirm = false;
                    }
                    break;
                case Hl7Result.QueryResultType.NotFound:
                    StatusMessage = "未找到匹配的记录";
                    StatusColor = new SolidColorBrush(Colors.Orange);
                    ResultVisibility = Visibility.Collapsed;
                    CanConfirm = false;
                    break;
                case Hl7Result.QueryResultType.WaitingTimeout:
                    StatusMessage = "查询等待超时，请重试";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                case Hl7Result.QueryResultType.Timeout:
                    StatusMessage = "查询超时，请重试";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                case Hl7Result.QueryResultType.NotConnected:
                    StatusMessage = "未连接到LIS系统，请检查网络连接";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    StatusMessage = "未知错误，请重试";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
            }
        }

        // 确认命令
        [RelayCommand]
        public void Confirm()
        {
            _actionConfirm?.Invoke(this, ApplyTestList.ToList());
        }

        // 取消命令
        [RelayCommand]
        public void Cancel()
        {
            _actionCancel?.Invoke(this);
        }

        // 关闭命令
        [RelayCommand]
        public void Close()
        {
            _actionClose?.Invoke(this);
        }
    }
} 
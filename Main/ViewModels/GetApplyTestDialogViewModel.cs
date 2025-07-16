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

        // �������Ϣ
        [ObservableProperty]
        private string title = "��ȡ������";

        // ��ѯ���Ϳ���
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool isTestNumQueryMode = true;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool isBarcodeQueryMode;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private bool isApplyTimeQueryMode;

        // ��ѯ�ı�
        [ObservableProperty]
        private string queryText = "";

        // ��ֹ��Ų�ѯ�ı�
        [ObservableProperty]
        private string endQueryText = "";

        // �ͼ�ʱ���ѯ
        [ObservableProperty]
        private DateTime? startApplyTime;

        [ObservableProperty]
        private DateTime? endApplyTime;

        // ˮӡ�ı�
        [ObservableProperty]
        private string startWatermarkText = "������������ʼ";

        [ObservableProperty]
        private string endWatermarkText = "������������ֹ";

        // ��ֹ��������ɼ���
        [ObservableProperty]
        private Visibility endVisibility = Visibility.Visible;

        // ʱ��ѡ�����ɼ���
        [ObservableProperty]
        private Visibility timePickerVisibility = Visibility.Collapsed;

        // �ı���ɼ���
        [ObservableProperty]
        private Visibility textBoxVisibility = Visibility.Visible;

        // ��ѯ״̬
        [ObservableProperty]
        private string statusMessage = "�������ѯ�����������ѯ��ť";

        [ObservableProperty]
        private SolidColorBrush statusColor = new SolidColorBrush(Colors.Black);

        // ����б�
        [ObservableProperty]
        private ObservableCollection<ApplyTest> applyTestList = new ObservableCollection<ApplyTest>();

        [ObservableProperty]
        private Visibility resultVisibility = Visibility.Collapsed;

        // �Ի���ť����
        [ObservableProperty]
        private string confirmText = "ȷ��ѡ��";

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private string cancelText = "ȡ��";

        [NotifyPropertyChangedRecipients]
        [ObservableProperty]
        private string closeText = "";

        [ObservableProperty]
        private Visibility showCancel;

        [ObservableProperty]
        private Visibility showClose;

        [ObservableProperty]
        private bool canConfirm = false;

        // �����ص�
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

        // ���Ա仯����
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
                    StartWatermarkText = "������������ʼ";
                    EndWatermarkText = "������������ֹ";
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
                    StartWatermarkText = "����������";
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
                    StatusMessage = "��ֹʱ�䲻��С����ʼʱ��";
                    StatusColor = new SolidColorBrush(Colors.Red);
                }
            }
        }

        // ��ѯ����
        [RelayCommand]
        private async Task Query()
        {
            if (IsApplyTimeQueryMode)
            {
                if (!StartApplyTime.HasValue)
                {
                    StatusMessage = "��ѡ����ʼʱ��";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    return;
                }
                if (!EndApplyTime.HasValue)
                {
                    StatusMessage = "��ѡ����ֹʱ��";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    return;
                }
                if (EndApplyTime.Value < StartApplyTime.Value)
                {
                    StatusMessage = "��ֹʱ�䲻��С����ʼʱ��";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    return;
                }
            }
            else if (string.IsNullOrWhiteSpace(QueryText))
            {
                StatusMessage = "�������ѯ����";
                StatusColor = new SolidColorBrush(Colors.Red);
                return;
            }

            StatusMessage = "���ڲ�ѯ...";
            StatusColor = new SolidColorBrush(Colors.Black);
            ResultVisibility = Visibility.Collapsed;
            CanConfirm = false;

            try
            {
                Hl7Result.QueryResult result;

                if (IsTestNumQueryMode)
                {
                    // ʹ��condition1��condition2
                    string condition2 = string.IsNullOrWhiteSpace(EndQueryText) ? QueryText : EndQueryText;
                    result = await lisRepository.QueryApplyTestFormTestNumAsync(QueryText, condition2);
                }
                else if (IsBarcodeQueryMode)
                {
                    // �����ѯֻʹ��condition1
                    result = await lisRepository.QueryApplyTestFormBarcodeAsync(QueryText);
                }
                else
                {
                    // �ͼ�ʱ���ѯ
                    result = await lisRepository.QueryApplyTestFormInspectDateAsync(StartApplyTime?.ToString(DateTimeEx.DateTimeFormat3), EndApplyTime?.ToString(DateTimeEx.DateTimeFormat3));
                }

                // �����ѯ���
                HandleQueryResult(result);
            }
            catch (Exception ex)
            {
                StatusMessage = $"��ѯ����: {ex.Message}";
                StatusColor = new SolidColorBrush(Colors.Red);
            }
        }

        // �����ѯ���
        private void HandleQueryResult(Hl7Result.QueryResult result)
        {
            switch (result.ResultType)
            {
                case Hl7Result.QueryResultType.Success:
                    if (result.ApplyTests != null && result.ApplyTests.Count > 0)
                    {
                        ApplyTestList = new ObservableCollection<ApplyTest>(result.ApplyTests);
                        StatusMessage = $"��ѯ�ɹ������ҵ� {result.ApplyTests.Count} ����¼";
                        StatusColor = new SolidColorBrush(Colors.Green);
                        ResultVisibility = Visibility.Visible;
                        CanConfirm = true;
                    }
                    else
                    {
                        StatusMessage = "��ѯ�ɹ�����δ�ҵ���¼";
                        StatusColor = new SolidColorBrush(Colors.Orange);
                        ResultVisibility = Visibility.Collapsed;
                        CanConfirm = false;
                    }
                    break;
                case Hl7Result.QueryResultType.NotFound:
                    StatusMessage = "δ�ҵ�ƥ��ļ�¼";
                    StatusColor = new SolidColorBrush(Colors.Orange);
                    ResultVisibility = Visibility.Collapsed;
                    CanConfirm = false;
                    break;
                case Hl7Result.QueryResultType.WaitingTimeout:
                    StatusMessage = "��ѯ�ȴ���ʱ��������";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                case Hl7Result.QueryResultType.Timeout:
                    StatusMessage = "��ѯ��ʱ��������";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                case Hl7Result.QueryResultType.NotConnected:
                    StatusMessage = "δ���ӵ�LISϵͳ��������������";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    StatusMessage = "δ֪����������";
                    StatusColor = new SolidColorBrush(Colors.Red);
                    break;
            }
        }

        // ȷ������
        [RelayCommand]
        public void Confirm()
        {
            _actionConfirm?.Invoke(this, ApplyTestList.ToList());
        }

        // ȡ������
        [RelayCommand]
        public void Cancel()
        {
            _actionCancel?.Invoke(this);
        }

        // �ر�����
        [RelayCommand]
        public void Close()
        {
            _actionClose?.Invoke(this);
        }
    }
} 
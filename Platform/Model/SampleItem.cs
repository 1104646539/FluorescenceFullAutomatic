using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FluorescenceFullAutomatic.Platform.Model
{
    public partial class SampleItem : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor("Color")]
        [NotifyPropertyChangedFor("BorderColor")]
        [JsonIgnore]
        private SampleState state;

        [ObservableProperty]
        [JsonIgnore]
        private SampleType sampleType;
        [ObservableProperty]
        [JsonIgnore]
        private int resultId;
        [ObservableProperty]
        [JsonIgnore]
        private TestResult testResult;
        public Brush Color
        {
            get
            {
                switch (State)
                {
                    case SampleState.None:
                        return Brushes.White;
                    case SampleState.Exist:
                        return Brushes.Gray;
                    case SampleState.ScanSuccess:
                    case SampleState.SqueezeCompleted:
                    case SampleState.PiercedCompleted:
                    case SampleState.SamplingCompleted:
                        return Brushes.Green;
                    case SampleState.NotExist:
                    case SampleState.ScanFailed:
                    case SampleState.SamplingFailed:
                        return Brushes.Red;
                    default:
                        return Brushes.White;
                }
            }
        }
        public Brush BorderColor
        {
            get
            {
                switch (State)
                {
                    case SampleState.None:
                        return Brushes.Gray;
                    case SampleState.Exist:
                        return Brushes.Gray;
                    case SampleState.ScanSuccess:
                    case SampleState.SqueezeCompleted:
                    case SampleState.PiercedCompleted:
                    case SampleState.SamplingCompleted:
                        return Brushes.Green;
                    case SampleState.NotExist:
                    case SampleState.ScanFailed:
                    case SampleState.SamplingFailed:
                        return Brushes.Red;
                    default:
                        return Brushes.Gray;
                }
            }
        }
        
    }

    public enum SampleType
    {
        [Description("未知")]
        None,

        [Description("样本杯")]
        SampleCup,

        [Description("样本管")]
        SampleTube,
    }

    public enum SampleState
    {
        [Description("未知")]
        None,

        [Description("存在")]
        Exist,

        [Description("不存在")]
        NotExist,

        [Description("扫码成功")]
        ScanSuccess,

        [Description("扫码失败")]
        ScanFailed,

        [Description("挤压完成")]
        SqueezeCompleted,

        [Description("刺破完成")]
        PiercedCompleted,

        [Description("取样完成")]
        SamplingCompleted,

        [Description("取样失败")]
        SamplingFailed,

    }
}

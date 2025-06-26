using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    public enum ResultState
    {
        [Description("未知")]
        None,

        [Description("扫码成功")]
        ScanSuccess,

        [Description("扫码失败")]
        ScanFailed,

        [Description("挤压完成")]
        SqueezeCompleted,

        [Description("刺破完成")]
        PiercedCompleted,

        [Description("取样失败")]
        SamplingFailed,

        [Description("取样完成")]
        SamplingSuccess,

        [Description("加样完成")]
        AddSampleSuccess,

        [Description("加样失败")]
        AddSampleFailed,

        [Description("孵育中")]
        Incubation,

        [Description("检测完成")]
        TestFinish
    }
}

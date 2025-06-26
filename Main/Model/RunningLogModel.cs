using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using SqlSugar;

namespace FluorescenceFullAutomatic.Model
{

    // [SugarTable(tableName: "runing_log", IsDisabledDelete = true)]
    public partial class RuningLogModel : ObservableObject
    {
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public RunningLogType type;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string msg;

      
    }
    public enum RunningLogType{
         [Description("提示")]
        Normal,
         [Description("警告")]
        Warning,
        [Description("错误")]
        Error,
        
    }
}

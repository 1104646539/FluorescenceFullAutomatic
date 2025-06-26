using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    [SugarTable(tableName: "patient", IsDisabledDelete = true)]
    public partial class Patient :ObservableObject
    {
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string patientName;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string patientGender;
        /// <summary>
        /// 年龄
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string patientAge;
        /// <summary>
        /// 送检日期
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public DateTime inspectDate;
        /// <summary>
        /// 送检科室
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string inspectDepartment;
        /// <summary>
        /// 送检医生
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string inspectDoctor;

        /// <summary>
        /// 检测医生
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string testDoctor;

        /// <summary>
        /// 审核医生
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string checkDoctor;

           
    }
}

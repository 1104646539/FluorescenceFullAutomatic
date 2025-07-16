using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Model
{
    [SugarTable(tableName: "apply_test", IsDisabledDelete = true)]
    public partial class ApplyTest : ObservableObject
    {

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string barcode;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string hospitalBarcode;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string testNum;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string projectCode;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public ApplyTestType applyTestType;


        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int patientId;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int testResultId;

        [ObservableProperty]
        [JsonIgnore]
        [property: SqlSugar.Navigate(SqlSugar.NavigateType.OneToOne,nameof(PatientId))]
        public Patient patient;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public bool isSelected;
        
    }
    public enum ApplyTestType
    {
        /// <summary>
        /// 等待检测
        /// </summary>
        WaitTest = 0,
        /// <summary>
        /// 检测完成
        /// </summary>
        TestEnd = 1,
        /// <summary>
        /// 全部
        /// </summary>
        All = 2,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using SqlSugar;

namespace FluorescenceFullAutomatic.Model
{
    [SugarTable(tableName: "test_result", IsDisabledDelete = true)]
    public partial class TestResult : ObservableObject
    {
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id;

        /// <summary>
        /// 条码
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string barcode;
        /// <summary>
        /// 检测卡条码
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string cardQRCode;
        /// <summary>
        /// 检测编号
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string testNum;
        /// <summary>
        /// 检测时处于卡仓的位置
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string frameNum;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string t;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string c;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string tc;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string con;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string result;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string t2;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string c2;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string tc2;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string con2;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string result2;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string testVerdict;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public DateTime testTime;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int projectId;

        [ObservableProperty]
        [JsonIgnore]
        [property: SqlSugar.Navigate(SqlSugar.NavigateType.OneToOne,nameof(ProjectId))]
        public Project project;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int pointId;

        [ObservableProperty]
        [JsonIgnore]
        [property: SqlSugar.Navigate(SqlSugar.NavigateType.OneToOne, nameof(PointId))]
        public Point point;

        
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int patientId;

        [ObservableProperty]
        [JsonIgnore]
        [property: SqlSugar.Navigate(SqlSugar.NavigateType.OneToOne,nameof(PatientId))]
        public Patient patient;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        ResultState resultState;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public bool isSelected;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public bool isUploaded;

        /// <summary>
        /// 克隆TestResult对象
        /// </summary>
        /// <param name="newId">新的ID，如果不传入则复制原ID</param>
        /// <returns>克隆的TestResult对象</returns>
        public TestResult Clone(int? newId = null)
        {
            return new TestResult
            {
            Id = newId ?? this.Id,
            Barcode = this.Barcode,
            CardQRCode = this.CardQRCode,
            TestNum = this.TestNum,
            FrameNum = this.FrameNum,
            TestTime = this.TestTime,
            T = this.T,
            C = this.C,
            Tc = this.Tc,
            Con = this.Con,
            Result = this.Result,
            T2 = this.T2,
            C2 = this.C2,
            Tc2 = this.Tc2,
            Con2 = this.Con2,
            Result2 = this.Result2,
            TestVerdict = this.TestVerdict,
            ProjectId = this.ProjectId,
            Project = this.Project?.Clone(),
            PointId = this.PointId,
            Point = this.Point?.Clone(),
            PatientId = this.PatientId,
            Patient = this.Patient?.Clone(),
            ResultState = this.ResultState,
            IsSelected = this.IsSelected,
            IsUploaded = this.IsUploaded
            };
        }
}
}

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
        /// ÌõÂë
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string barcode;
        /// <summary>
        /// ¼ì²â¿¨ÌõÂë
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string cardQRCode;
        /// <summary>
        /// ¼ì²â±àºÅ
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string testNum;
        /// <summary>
        /// ¼ì²âÊ±´¦ÓÚ¿¨²ÖµÄÎ»ÖÃ
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
    }
}

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
    [SugarTable(tableName: "project", IsDisabledDelete = true)]
    public partial class Project : ObservableObject
    {
        /// <summary>
        /// 0 单联卡
        /// </summary>
        public const int Project_Type_Single = 0;
        /// <summary>
        /// 1 双联卡
        /// </summary>
        public const int Project_Type_Double = 1;

        /// <summary>
        /// 0 普通检测卡
        /// </summary>
        public const int Test_Type_Stadard = 0;
        /// <summary>
        /// 1 质控卡
        /// </summary>
        public const int Test_Type_QC = 1;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string projectName;
        /// <summary>
        /// 批号
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string batchNum;
        /// <summary>
        /// 识别码
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string identifierCode;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string projectCode;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string projectUnit;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int projectLjz;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double a1;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double a2;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double x0;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double p;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string projectUnit2;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int projectLjz2;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int conMax;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double a12;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double a22;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double x02;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public double p2;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int conMax2;
        /// <summary>
        /// 项目类型 
        /// 0:单联卡
        /// 1:双联卡
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int projectType;
        /// <summary>
        /// 检测类型
        /// 0:检测
        /// 1:质控
        /// </summary>
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int testType;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public int isDefault;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string scanStart;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string scanEnd;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string peakWidth;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        public string peakDistance;


        /// <summary>
        /// 克隆Project对象
        /// </summary>
        /// <param name="newId">新的ID，如果不传入则复制原ID</param>
        /// <returns>克隆的Project对象</returns>
        public Project Clone(int? newId = null)
        {
            return new Project
            {
                Id = newId ?? this.Id,
                ProjectName = this.ProjectName,
                BatchNum = this.BatchNum,
                IdentifierCode = this.IdentifierCode,
                ProjectCode = this.ProjectCode,
                ProjectUnit = this.ProjectUnit,
                ProjectLjz = this.ProjectLjz,
                A1 = this.A1,
                A2 = this.A2,
                X0 = this.X0,
                P = this.P,
                ProjectUnit2 = this.ProjectUnit2,
                ProjectLjz2 = this.ProjectLjz2,
                ConMax = this.ConMax,
                A12 = this.A12,
                A22 = this.A22,
                X02 = this.X02,
                P2 = this.P2,
                ConMax2 = this.ConMax2,
                ProjectType = this.ProjectType,
                TestType = this.TestType,
                IsDefault = this.IsDefault,
                ScanStart = this.ScanStart,
                ScanEnd = this.ScanEnd,
                PeakWidth = this.PeakWidth,
                PeakDistance = this.PeakDistance    
            };
        }
    }
}

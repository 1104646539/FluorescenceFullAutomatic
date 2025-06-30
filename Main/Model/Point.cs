using CommunityToolkit.Mvvm.ComponentModel;
using FluorescenceFullAutomatic.Converters;
using Newtonsoft.Json;
using SqlSugar;
using SqlSugar.DbConvert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    [SugarTable(tableName: "point", IsDisabledDelete = true)]

    public partial class Point : ObservableObject
    {
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int id;
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(ColumnDataType = "varchar(5000)", SqlParameterDbType = typeof(IntArrayToStringConvert))]
        public int[] points;

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
        [property: SugarColumn(ColumnDataType = "varchar(50)", SqlParameterDbType = typeof(IntArrayToStringConvert))]
        public int[] location;
        public Point()
        {
            
        }

        /// <summary>
        /// 克隆Point对象
        /// </summary>
        /// <param name="newId">新的ID，如果不传入则复制原ID</param>
        /// <returns>克隆的Point对象</returns>
        public Point Clone(int? newId = null)
        {
            return new Point
            {
                Id = newId ?? this.Id,
                Points = this.Points,   
                T = this.T, 
                C = this.C,
                Tc = this.Tc,
                T2 = this.T2,
                C2 = this.C2,
                Tc2 = this.Tc2,
                Location = this.Location    
            };
        }
    }
}

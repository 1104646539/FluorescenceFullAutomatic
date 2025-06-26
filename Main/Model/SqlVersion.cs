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
    [SugarTable(tableName: "sql_version", IsDisabledDelete = true)]
    public partial class SqlVersion : ObservableObject
    {
        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        private int id;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        private string name;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        private int exist;

        [ObservableProperty]
        [JsonIgnore]
        [property: SugarColumn(IsNullable = true)]
        private DateTime insertTime;

    }
}

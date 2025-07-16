using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Sql;
using FluorescenceFullAutomatic.Platform.Utils;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class FilterConditionViewModel:ObservableObject
    {
        /// <summary>
        /// 筛选条件
        /// </summary>
        [ObservableProperty]
        public ConditionModel condition;

        /// <summary>
        /// 确认
        /// </summary>  
        public Action<ConditionModel> confirmAction;

        /// <summary>   
        /// 取消
        /// </summary>
        public Action cancelAction;

        [ObservableProperty]
        public List<string> testVerdictList;

        [ObservableProperty]
        public List<string> projectNameList;

        public FilterConditionViewModel(){
            testVerdictList = new List<string>(){"全部",GlobalUtil.GetString(Keys.ResultNegative),
            GlobalUtil.GetString(Keys.ResultPositive),
            GlobalUtil.GetString(Keys.ResultInvalid)
            };
            projectNameList = new List<string>();
            projectNameList.Add("全部");
            projectNameList.AddRange(SqlHelper.projects);
            projectNameList.AddRange(SqlHelper.projects2);
        }

        public void Update(ConditionModel condition){
            this.Condition = condition;
        }

        [RelayCommand]
        public void Confirm(){
            confirmAction?.Invoke(Condition);
        }

        [RelayCommand]
        public void Cancel(){
            cancelAction?.Invoke();
        }
        [RelayCommand]
        public void Clear(){
            Condition = new ConditionModel();
        }
    }
}

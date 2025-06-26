using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Model;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.ViewModels
{

    public partial class ReactionAreaViewModel:ObservableObject
    {
        [ObservableProperty]
        ObservableCollection<ObservableCollection<ReactionAreaItem>> reactionAreaItems;
        const int ReactionAreaMaxX = 10;
        const int ReactionAreaMaxY = 3;
        private static ReactionAreaViewModel _Instance;
        public static ReactionAreaViewModel Instance =>_Instance ??= new ReactionAreaViewModel();
      
        private ReactionAreaViewModel() {
            ReactionAreaItems = new ObservableCollection<ObservableCollection<ReactionAreaItem>>();
            InitState();
        }
        public void UpdateItem(int y,int x,Func<ReactionAreaItem,ReactionAreaItem> func) {
            ReactionAreaItems[y][x] = func(ReactionAreaItems[y][x]);
        }
        public void Clear(){
            ReactionAreaItems.Clear();
            InitState();
        }
        public void InitState(){
             for (int i = 0; i < ReactionAreaMaxY; i++)
            {
                ObservableCollection<ReactionAreaItem> items = new ObservableCollection<ReactionAreaItem>();
                for (int j = 0; j < ReactionAreaMaxX; j++)
                {
                    ReactionAreaItem item = new ReactionAreaItem();
                    items.Add(item);
                }
                ReactionAreaItems.Add(items);
            }
        }

        public ReactionAreaItem GetItem(int reactionAreaY, int reactionAreaX)
        {
            return ReactionAreaItems[reactionAreaY][reactionAreaX];
        }
        
        [RelayCommand]
        public void ClickItem(ReactionAreaItem item) {
            Log.Information("点击反应区 " + item.ReactionAreaY + " " + item.ReactionAreaX + " " + JsonConvert.SerializeObject(item));
        }
        /// <summary>
        /// 获取反应区下一个可用位置
        /// </summary>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public bool GetReactionAreaNext(out int y,out int x) {
            for(int i = 0; i < ReactionAreaMaxY; i++){
                for(int j = 0; j < ReactionAreaMaxX; j++){
                    if(ReactionAreaItems[i][j].State == ReactionAreaItem.STATE_EMPTY
                    || ReactionAreaItems[i][j].State == ReactionAreaItem.STATE_END){
                        y = i;
                        x = j;
                        return true;
                    }
                }
            }
            y = -1;
            x = -1;
            return false;
        }
       
    }
}

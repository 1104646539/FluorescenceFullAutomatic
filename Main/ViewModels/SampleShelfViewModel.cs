using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluorescenceFullAutomatic.Platform.Model;
using Newtonsoft.Json;
using Serilog;

namespace FluorescenceFullAutomatic.ViewModels
{
    public partial class SampleShelfViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ObservableCollection<SampleItem>> sampleItems;

        public event Action<SampleItem> onSelectedSampleItem;


        public SampleShelfViewModel()
        {
            SampleItems = new ObservableCollection<ObservableCollection<SampleItem>>();

            InitState();
            
        }

        public void UpdateSampleItems(int row, int col, Func<SampleItem, SampleItem> func)
        {
            SampleItems[row][col] = func(SampleItems[row][col]);
        }

        public SampleItem GetSampleItem(int row, int col)
        {
            return SampleItems[row][col];
        }
        public bool GetTestResultIndex(int id,out int row,out int col){
            row = -1;
            col = -1;
            for (int i = 0; i < SampleItems.Count; i++)
            {
                for (int j = 0; j < SampleItems[i].Count; j++)
                {
                    if (SampleItems[i][j] != null && SampleItems[i][j].ResultId == id) {
                        row = i;
                        col = j;
                        return true;
                    }
                }
            }
            return false;
        }
                public ObservableCollection<ObservableCollection<SampleItem>> GetSampleItems()
        {
            return SampleItems;
        }

        [RelayCommand]
        public void ClickSampleItem(SampleItem item)
        {
            Log.Information("点击样本 " + JsonConvert.SerializeObject(item));
            onSelectedSampleItem?.Invoke(item);
        }

        public void Clear()
        {
            SampleItems.Clear();
            InitState();
        }
        bool[] shelfStates = new bool[] { false, false, false, false, false , false };
        public void UpdateShelfState(bool[] shelfStates)
        {
            this.shelfStates = shelfStates;
            Clear();
        }
        private void InitState()
        {
            for (int i = 0; i < 6; i++)
            {
                ObservableCollection<SampleItem> items = new ObservableCollection<SampleItem>();
                if (shelfStates[i]) { 
                for (int j = 0; j < 5; j++)
                {
                    SampleItem item = new SampleItem();
                    items.Add(item);
                }
                }
                SampleItems.Add(items);
            }
        }
    }
}

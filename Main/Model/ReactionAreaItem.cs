using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// ·´Ó¦Çø¿¨
    /// </summary>
    public partial class ReactionAreaItem : ObservableObject
    {
        public const int STATE_EMPTY = 0; // ¿Õ
        public const int STATE_WAIT = 1; // µÈ´ý¼ì²â
        public const int STATE_END = 2; // ¼ì²â½áÊø
        [ObservableProperty]
        [NotifyPropertyChangedFor("Color")]
        [JsonIgnore]
        public int state;

        [ObservableProperty]
        [JsonIgnore]
        public int reactionAreaX;
        [ObservableProperty]
        [JsonIgnore]
        public int reactionAreaY;

        [ObservableProperty]
        [JsonIgnore]
        public TestResult testResult;

        [ObservableProperty]
        [JsonIgnore]
        public long enqueueTime;
        public ReactionAreaItem()
        {
            state = STATE_EMPTY;
        }
        public Brush Color { get {
                switch (State)
                {
                    case STATE_WAIT:
                        return Brushes.Green;
                    case STATE_END:
                        return Brushes.AliceBlue;
                    default:
                        return Brushes.Gray;
                }
            } }

    }
}

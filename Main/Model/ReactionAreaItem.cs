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
    /// ��Ӧ����
    /// </summary>
    public partial class ReactionAreaItem : ObservableObject
    {
        public const int STATE_EMPTY = 0; // ��
        public const int STATE_WAIT = 1; // �ȴ����
        public const int STATE_END = 2; // ������
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

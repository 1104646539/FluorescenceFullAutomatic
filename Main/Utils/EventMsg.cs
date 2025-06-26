using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Utils
{
    public class EventMsg<T> : ValueChangedMessage<T>
    {
        public int What { get; set; }

        public EventMsg(T value) : base(value)
        {
        }
    }
}

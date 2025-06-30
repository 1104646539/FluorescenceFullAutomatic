using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Services
{
    public interface IDispatcherService
    {
        public void Invoke(Action action);
    }
    public class DispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
        {
            if (action == null) return;
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IDispatcherService
    {
        public void Invoke(Action action);
        public void InvokeAsync(Action action);
    }
    public class DispatcherService : IDispatcherService
    {
        
       public DispatcherService(){
            ThreadPool.SetMaxThreads(100, 100);
        }
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
        public void InvokeAsync(Action action)
        {
            if (action == null) return;
            ThreadPool.QueueUserWorkItem(state => {
                action.Invoke();
            });
        }
    }
}

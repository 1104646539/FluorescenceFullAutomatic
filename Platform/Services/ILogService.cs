using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface ILogService
    {
        void Info(string msg, string tag = "");
        void Error(string msg, string tag = "");
        void Warning(string msg, string tag = "");
    }

    public class LogService : ILogService
    {
        public LogService() { }
        public void Error(string msg, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Serilog.Log.Error(msg);
            }
            else
            {
                Serilog.Log.Error("{Tag}, {Msg}", tag, msg);
            }
        }

        public void Info(string msg, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Serilog.Log.Information(msg);
            }
            else
            {
                Serilog.Log.Information("{Tag}, {Msg}", tag, msg);
            }
        }

        public void Warning(string msg, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Serilog.Log.Warning(msg);
            }
            else
            {
                Serilog.Log.Warning("{Tag}, {Msg}", tag, msg);
            }
        }
    }
}

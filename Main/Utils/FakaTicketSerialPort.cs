using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;

namespace FluorescenceFullAutomatic.Utils
{
    public class FakaTicketSerialPort : ISerialPort
    {
        private bool _isConnected;
        public event Action<byte[]> DataReceived;
        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortConnectExceptionReceived;
        public event Action<string> SerialPortExceptionReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;

       
      
        public void SendData(string data)
        {
            
        }

        public void SendData(byte[] data)
        {
            if(data!=null && data.Length == 2){
                if(data[0 ] == TicketReportUtil.GetStatusCommand[0] && data[1 ] == TicketReportUtil.GetStatusCommand[1]){
                    DataReceived?.Invoke(new byte[]{TicketReportUtil.PageFull});//有纸

                    // DataReceived?.Invoke(new byte[]{TicketReportUtil.PageOut});//没纸
                }
            }
        }
         public void Connect(string portName, int baudRate)
        {
           
            _isConnected = true;
            SerialPortConnectReceived?.Invoke("");
        }

        public void Disconnect()
        {
            _isConnected = false;
        
        }

        public bool IsOpen()
        {
            return _isConnected;
        }
    }
}

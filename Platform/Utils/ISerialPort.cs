using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Platform.Utils
{
    public interface ISerialPort
    {

        event Action<byte[]> DataReceived;
        event Action<string> SerialPortConnectReceived;
        event Action<string> SerialPortExceptionReceived;
        event Action<string> SerialPortOriginDataReceived;
        event Action<string> SerialPortOriginDataSend;
        void Connect(string portName, int baudRate);
        void SendData(string data);
        void SendData(byte[] data);
        void Disconnect();
        bool IsOpen();
    }
 
}

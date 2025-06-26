using FluorescenceFullAutomatic.Config;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Utils
{
    public class SerialPortImpl : ISerialPort
    {
        public event Action<byte[]> DataReceived;
        public event Action<string> SerialPortConnectReceived;
        public event Action<string> SerialPortConnectExceptionReceived;
        public event Action<string> SerialPortOriginDataReceived;
        public event Action<string> SerialPortOriginDataSend;
        public event Action<string> SerialPortExceptionReceived;

        bool _isOpen = false;
        public SerialPort SerialPort { get; set; }

        public SerialPortImpl()
        {
            SerialPort = new SerialPort();
            
        }   

        public void Connect(string portName, int baudRate)
        {
           
            try
            {
                
                SerialPort.PortName = portName;
                SerialPort.BaudRate = baudRate;
                SerialPort.StopBits = StopBits.One;
                SerialPort.DataBits = 8;
                SerialPort.Parity = Parity.None;
                SerialPort.Open();
                SerialPortConnectReceived?.Invoke("");
               _isOpen = true;
            }
            catch (Exception e) {
                SerialPortConnectReceived?.Invoke("´ò¿ªÊ§°Ü "+ e.Message);
                _isOpen = false;
            }
            SerialPort.ErrorReceived += (s, e) =>
            {
                SerialPortConnectExceptionReceived?.Invoke(e.EventType.ToString());
                 _isOpen = false;
            };
            
            SerialPort.DataReceived += (s, e) =>
            {
                byte[] buffer = new byte[SerialPort.BytesToRead];
                SerialPort.Read(buffer, 0, buffer.Length);
                DataReceived?.Invoke(buffer);
                SerialPortOriginDataReceived?.Invoke(SerialGlobal.Encoding.GetString(buffer)/*.Replace(SerialGlobal.EndStr,"")*/);
            };
        }

        public void Disconnect()
        {
             _isOpen = false;
            SerialPort.Close();
            SerialPort.Dispose();
        }

        public void SendData(byte[] data)
        {
            //SerialPortOriginDataSend?.Invoke(data);
            Log.Information("SendData data");
            SerialPort.Write(data, 0, data.Length);
        }

        public void SendData(string data)
        {
            SerialPortOriginDataSend?.Invoke(data);
            SerialPort.Write(data);
        }
        
        public bool IsOpen()
        {
            return _isOpen;
        }
    }
}

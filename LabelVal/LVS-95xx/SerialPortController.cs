using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx
{
    public class SerialPortController
    {
        public delegate void PacketAvailableDelegate(string packet);
        public event PacketAvailableDelegate PacketAvailable;

        public event EventHandler Exception;

        public List<string> COMPortsAvailable { get; } = new List<string>();

        private System.IO.Ports.SerialPort SerialPort { get; set; } = new System.IO.Ports.SerialPort();

        private bool running;
        private bool listening;

        public void GetCOMList()
        {
            COMPortsAvailable.Clear();

            foreach (var name in System.IO.Ports.SerialPort.GetPortNames())
                COMPortsAvailable.Add(name);
        }

        public void Init(string portName)
        {
            SerialPort.PortName = portName;
            SerialPort.BaudRate = 9600;
            SerialPort.Parity = Parity.None;
            SerialPort.DataBits = 8;
            SerialPort.StopBits = StopBits.One;
            SerialPort.Handshake = Handshake.None;

            SerialPort.ReadTimeout = 500;
            SerialPort.WriteTimeout = 500;

            SerialPort.ErrorReceived -= SerialPort_ErrorReceived;
            SerialPort.ErrorReceived += SerialPort_ErrorReceived;
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) => Disconnect();

        public bool Connect()
        {
            if (!SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Open();
                    Task.Run(() => ReadThread());
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        public async void Disconnect()
        {
            if (SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Close();
                    await Task.Run(() => { listening = false; while (running) { } });
                }
                catch { }
            }

            running = false;
            listening = false;
        }


        public void ReadThread()
        {
            running = true;
            listening = true;

            StringBuilder sb = new StringBuilder();
            DateTime start = DateTime.MaxValue;
            while (listening)
            {
                try
                {
                    string message = SerialPort.ReadExisting();

                    if (!string.IsNullOrEmpty(message))
                    {
                        sb.Append(message);
                        start = DateTime.Now;
                    }
                    else if(sb.Length > 0 && (DateTime.Now - start) > TimeSpan.FromMilliseconds(500))
                    {
                        
                        PacketAvailable?.Invoke(sb.ToString());
                        sb.Clear();
                    }
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    Task.Run(() => Exception?.Invoke(ex, new EventArgs()));

                    Disconnect();
                }

                Thread.Sleep(1);
            }
            running = false;
        }


    }
}

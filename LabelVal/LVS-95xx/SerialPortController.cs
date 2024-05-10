using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx
{
    public class SerialPortController
    {
        public delegate void DataAvailableDelegate(string data);
        public event DataAvailableDelegate DataAvailable;

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
        }
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
                }
                catch { }

                await Task.Run(() => { listening = false; while (running) { } });
            }
        }

        public void ReadThread()
        {
            listening = true;
            running = true;

            while (listening)
            {
                try
                {
                    string message = SerialPort.ReadExisting();

                    if (!string.IsNullOrEmpty(message))
                        DataAvailable?.Invoke(message);
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    Task.Run(() => Exception?.Invoke(ex, new EventArgs()));

                    Disconnect();

                    listening = false;
                }

                Thread.Sleep(1);
            }
            running = false;
        }

    }
}

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx
{
    internal class LVS95xx_SerialPortSetup
    {
        public delegate void DataAvailableDelegate(string data);
        public event DataAvailableDelegate DataAvailable;


        private object WriteLock = new object();
        private string WriteData;

        public bool StartCancellableProcess(string file, string args, CancellationToken token)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = file,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var process = new Process { StartInfo = processInfo };

            process.Start();

            Task.Run(() => { ProcessLoop(process); });

            using (var waitHandle = new SafeWaitHandle(process.Handle, false))
            {
                using (var processFinishedEvent = new ManualResetEvent(false))
                {
                    processFinishedEvent.SafeWaitHandle = waitHandle;
                    int index = WaitHandle.WaitAny(new[] { processFinishedEvent, token.WaitHandle });
                    if (index == 1)
                    {
                        process.Kill();
                        Debug.WriteLine($"Process {file} {args} is terminated");
                        return false;
                    }
                }
            }
            return !token.IsCancellationRequested;
        }

        public void Write(string data)
        {
            lock (WriteLock)
                WriteData = data;

            while (!string.IsNullOrEmpty(WriteData)) { Thread.Sleep(1); }
        }

        private void ProcessLoop(Process process)
        {
            using (StreamWriter inputWriter = process.StandardInput)
            {
                using (StreamReader outputReader = process.StandardOutput)
                {
                    Task.Run(() =>
                    {
                        while (!process.HasExited)
                        {
                            string data = "";
                            int rd;
                            if ((rd = outputReader.Read()) > -1)
                                data += (char)rd;

                            if (!string.IsNullOrEmpty(data))
                                DataAvailable.Invoke(data);
                        }
                    });

                    while (!process.HasExited)
                    {


                        if (!string.IsNullOrEmpty(WriteData))
                        {
                            lock (WriteLock)
                            {
                                inputWriter.Write(WriteData);
                                WriteData = "";
                            }

                        }
                    }
                    //}
                }
            }
        }
    }
}

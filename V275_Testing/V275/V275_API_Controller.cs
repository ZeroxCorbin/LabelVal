using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using V275_Testing.V275.Models;

namespace V275_Testing.V275
{
    public class V275_API_Controller
    {
        public V275_API_Commands Commands { get; }= new V275_API_Commands();
        public V275_API_WebSocketEvents WebSocket { get; } = new V275_API_WebSocketEvents();

        public V275_Events_System SetupDetectEvent { get; private set; }

        public string Host { get => Commands.Host; set => Commands.Host = value; }
        public uint SystemPort { get => Commands.SystemPort; set => Commands.SystemPort = value; }
        public uint NodeNumber { get => Commands.NodeNumber; set => Commands.NodeNumber = value; }

        public string Status
        {
            get { return _Status; }
            set { _Status = value; }
        }
        private string _Status;



        public V275_API_Controller()
        {
            //WebSocket.SetupCapture += WebSocket_SetupCapture;
            WebSocket.SessionStateChange += WebSocket_SessionStateChange;
            //WebSocket.Heartbeat += WebSocket_Heartbeat;
            //WebSocket.SetupDetect += WebSocket_SetupDetect;
            WebSocket.LabelEnd += WebSocket_LabelEnd;
            WebSocket.SetupDetect += WebSocket_SetupDetect;
            //WebSocket.StateChange += WebSocket_StateChange;
        }

        bool SetupDetectEnd = false;
        private void WebSocket_SetupDetect(Models.V275_Events_System ev, bool end)
        {
            SetupDetectEvent = ev;
            SetupDetectEnd = end;
        }

        bool LabelEnd = false;
        private void WebSocket_LabelEnd(Models.V275_Events_System ev)
        {
            LabelEnd = true;
        }

        private void WebSocket_SessionStateChange(Models.V275_Events_System ev)
        {
            
        }

        public async Task<bool> GetJob()
        {
            if (!await Commands.GetJob())
            {
                Status = Commands.Status;
                return false;
            }
            return true;
        }

        public async Task<bool> Inspect(int repeat)
        {
            if (repeat > 0)
                if (!await Commands.SetRepeat(repeat))
                {
                    Status = Commands.Status;
                    return false;
                }

            if (!await Commands.Inspect())
            {
                Status = Commands.Status;
                return false;
            }

            LabelEnd = false;
            while(!LabelEnd) 
                Thread.Sleep(10);

            return true;
        }

        public async Task<bool> GetReport(int repeat)
        {
            if (!await Commands.GetReport())
            {
                Status = Commands.Status;
                return false;
            }

            if (!await Commands.GetRepeatsImage(repeat))
            {
                if (!Commands.Status.StartsWith("Gone"))
                {
                    Status = Commands.Status;
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> DeleteSectors()
        {
            if (!await Commands.GetJob())
            {
                Status = Commands.Status;
                return false;
            }

            foreach (var sec in Commands.Job.sectors)
            {
                if (!await Commands.DeleteSector(sec.name))
                {
                    Status = Commands.Status;
                    return false;
                }
            }

            return true;
        }
        public async Task<bool> DetectSectors()
        {
            if (!await Commands.GetDetect())
            {
                Status = Commands.Status;
                return false;
            }

            if (!await Commands.Detect())
            {
                Status = Commands.Status;
                return false;
            }

            SetupDetectEnd = false;
            await Task.Run(() => { while (!SetupDetectEnd) { } });

            return true;
        }

        public async Task<bool> AddSector(string name, string json)
        {
            if (!await Commands.AddSector(name, json))
            {
                Status = Commands.Status;
                return false;
            }
           else
                return true;
        }

        public List<V275_Job_Sector_Verify> CreateSectors(V275_Events_System ev, string gradingStandard)
        {
            int d1 = 1;
            int d2 = 1;

            List<V275_Job_Sector_Verify> lst = new List<V275_Job_Sector_Verify>();
            

            foreach (var val in ev.data.detections)
            {
                V275_Job_Sector_Verify verify = new V275_Job_Sector_Verify();

                if (gradingStandard.StartsWith("GS1"))
                {
                    verify.gradingStandard.enabled = true;
                    verify.gradingStandard.tableId = gradingStandard.Replace("GS1 TABLE ", "");
                }
                else
                {
                    verify.gradingStandard.enabled = false;
                    verify.gradingStandard.tableId = "1";
                }

                V275_Symbologies.Symbol sym = Commands.Symbologies.Find((e) => e.symbology == val.symbology);

                if (sym == null)
                    continue;

                if (sym.regionType == "verify1D")
                    verify.id = d1++;
                else
                    verify.id = d2++;

                verify.type = sym.regionType;
                verify.symbology = val.symbology;
                verify.name = $"{sym.regionType}_{verify.id}";
                verify.username = $"{char.ToUpper(verify.name[0])}{verify.name.Substring(1)}";

                verify.top = val.region.y;
                verify.left = val.region.x;
                verify.height = val.region.height;
                verify.width = val.region.width;

                verify.orientation = val.orientation;

                lst.Add(verify);
            }

            return lst;
        }

        private void read()
        {


        }

    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V275_Testing.V275.Models;

namespace V275_Testing.V275
{
    public class V275_API_Commands
    {
        V275_API_Connection Connection { get; set; } = new V275_API_Connection();
        V275_API_URLs URLs { get; set; } = new V275_API_URLs();

        //public bool IsLogggedIn { get; set; }
        //public bool IsMonitor { get; set; }
        public string Token { get; set; }
        public string Host { get => URLs.Host; set=>URLs.Host = value; }
        public string SystemPort { get => URLs.SystemPort; set => URLs.SystemPort = value; }
        public string NodeNumber { get => URLs.NodeNumber; set => URLs.NodeNumber = value; }

        //public bool IsException => Connection.IsException ? true : string.IsNullOrEmpty(Status) ? true : false;
        //public string Exception => Connection.IsException ? Connection.Exception.Message : Status;
        public string Status { get; private set; }

        public V275_Devices Devices { get; private set; }
        public V275_GradingStandards GradingStandards { get; private set; }
        public V275_Job Job { get; private set; }
        public V275_Report Report { get; private set; }

        private bool CheckResults(string json, bool ignoreJson = false)
        {
            Status = "";

            if(!ignoreJson)
                if(json != null)
                    if (!json.StartsWith("{"))
                        if (!json.StartsWith("["))
                            Status = $"Return data is not JSON: \"{json}\"";

            if (Connection.IsException)
                Status = Connection.Exception.Message;
            else if (!Connection.HttpResponseMessage.IsSuccessStatusCode)
                Status = $"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}";


            return string.IsNullOrEmpty(Status);
        }

        public async Task<bool> GetDevices()
        {
            string data = await Connection.Get(URLs.Devices(), "");

            bool res;
            if (res = CheckResults(data))
                Devices = JsonConvert.DeserializeObject<V275_Devices>(data);

            return res;
        }

        public async Task<bool> Login(string user, string pass, bool monitor, bool temporary = false)
        {
            Token = await Connection.Get_Token(URLs.Login(monitor, temporary), user, pass);

            return CheckResults(Token, true); ;
        }

        public async Task<bool> Logout()
        {
            await Connection.Put(URLs.Logout(), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> GetGradingStandards()
        {
            string result = await Connection.Get(URLs.GradingStandards(), Token);

            bool res;
            if (res = CheckResults(result))
                GradingStandards = JsonConvert.DeserializeObject<V275_GradingStandards>(result);

            return res;
        }

        public async Task<bool> GetJob()
        {
            string result = await Connection.Get(URLs.Job(), Token);

            bool res;
            if (res = CheckResults(result))
                Job = JsonConvert.DeserializeObject<V275_Job>(result);

            return res;
        }

        public async Task<bool> GetReport()
        {
            string result = await Connection.Get(URLs.Report(), Token);

            bool res;
            if (res = CheckResults(result))
                Report = JsonConvert.DeserializeObject<V275_Report>(result);

            return res;
        }

        public async Task<bool> Inspect()
        {
            await Connection.Put(URLs.Inspect(),"", Token);

            return CheckResults("", true);
        }
    }
}

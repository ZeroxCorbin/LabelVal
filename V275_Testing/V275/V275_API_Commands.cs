using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using V275_Testing.V275.Models;

namespace V275_Testing.V275
{
    public class V275_API_Commands
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private class Results
        {
            public bool OK { get; set; }
            public string Data { get; set; }
        }

        private V275_API_Connection Connection { get; set; } = new V275_API_Connection();
        public V275_API_URLs URLs { get; private set; } = new V275_API_URLs();

        //public bool IsLoggedIn { get; set; }
        //public bool IsMonitor { get; set; }
        public string Token { get; set; }
        public string Host { get => URLs.Host; set => URLs.Host = value; }
        public uint SystemPort { get => URLs.SystemPort; set => URLs.SystemPort = value; }
        public uint NodeNumber { get => URLs.NodeNumber; set => URLs.NodeNumber = value; }

        //public bool IsException => Connection.IsException ? true : string.IsNullOrEmpty(Status) ? true : false;
        //public string Exception => Connection.IsException ? Connection.Exception.Message : Status;
        public string Status { get; private set; }

        public V275_Devices Devices { get; private set; }
        public V275_Product Product { get; private set; }
        public V275_GradingStandards GradingStandards { get; private set; }
        public List<V275_Symbologies.Symbol> Symbologies { get; private set; }
        public V275_Job Job { get; private set; }
        public V275_Report Report { get; private set; }
        public V275_Configuration_Camera ConfigurationCamera { get; private set; }
        public V275_DetectResponse Detected { get; private set; }
        public List<int> Available { get; private set; }
        public byte[] Repeatimage { get; private set; }

        private bool CheckResults(string json, bool ignoreJson = false)
        {
            Status = "";

            if (!ignoreJson)
                if (json != null)
                    if (!json.StartsWith("{"))
                        if (!json.StartsWith("["))
                        {
                            Status = $"Return data is not JSON: \"{json}\"";
                            Logger.Warn($"Return data is not JSON: \"{json}\"");
                        }

            if (Connection.IsException)
            {
                Status = Connection.Exception.Message;
                Logger.Error(Connection.Exception);
            }

            else if (Connection.HttpResponseMessage != null && !Connection.HttpResponseMessage.IsSuccessStatusCode)
            {
                Status = $"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}";
                Logger.Warn($"{Connection.HttpResponseMessage.StatusCode}: {Connection.HttpResponseMessage.ReasonPhrase}");
            }

            return string.IsNullOrEmpty(Status);
        }

        public async Task<bool> GetDevices()
        {
            Logger.Info("GET: {url}", URLs.Devices());

            string data = await Connection.Get(URLs.Devices(), "");

            bool res;
            if (res = CheckResults(data))
                Devices = JsonConvert.DeserializeObject<V275_Devices>(data);

            return res;
        }

        public async Task<bool> GetProduct()
        {
            Logger.Info("GET: {url}", URLs.Product());

            string data = await Connection.Get(URLs.Product(), "");

            bool res;
            if (res = CheckResults(data))
                Product = JsonConvert.DeserializeObject<V275_Product>(data);

            return res;
        }

        public async Task<bool> Login(string user, string pass, bool monitor, bool temporary = false)
        {
            Logger.Info("LOGIN {user}: {url}", user, URLs.Login(monitor, temporary));

            Token = await Connection.Get_Token(URLs.Login(monitor, temporary), user, pass);

            return CheckResults(Token, true); ;
        }
        public async Task<bool> Logout()
        {
            Logger.Info("LOGOUT: {url}", URLs.Logout());

            await Connection.Put(URLs.Logout(), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> GetGradingStandards()
        {
            Logger.Info("GET: {url}", URLs.GradingStandards());

            string result = await Connection.Get(URLs.GradingStandards(), Token);

            bool res;
            if (res = CheckResults(result))
                GradingStandards = JsonConvert.DeserializeObject<V275_GradingStandards>(result);

            return res;
        }
        public async Task<bool> GetSymbologies()
        {
            Logger.Info("GET: {url}", URLs.VerifySymbologies());

            string result = await Connection.Get(URLs.VerifySymbologies(), Token);

            bool res;
            if (res = CheckResults(result))
                Symbologies = JsonConvert.DeserializeObject<List<V275_Symbologies.Symbol>>(result);

            return res;
        }
        public async Task<bool> GetJob()
        {
            Logger.Info("GET: {url}", URLs.Job());

            string result = await Connection.Get(URLs.Job(), Token);

            bool res;
            if (res = CheckResults(result))
                Job = JsonConvert.DeserializeObject<V275_Job>(result);

            return res;
        }
        public async Task<bool> GetReport()
        {
            Logger.Info("GET: {url}", URLs.Report());

            string result = await Connection.Get(URLs.Report(), Token);

            bool res;
            if (res = CheckResults(result))
                Report = JsonConvert.DeserializeObject<V275_Report>(result);

            return res;
        }
        public async Task<bool> GetRepeatsAvailable()
        {
            Logger.Info("GET: {url}", URLs.Available());

            string result = await Connection.Get(URLs.Available(), Token);

            bool res;
            if (res = CheckResults(result))
                Available = JsonConvert.DeserializeObject<int[]>(result).ToList();

            return res;
        }
        public async Task<bool> GetRepeatsImage(int repeat)
        {
            Logger.Info("GET: {url}", URLs.RepeatImage(repeat));

            var result = await Connection.GetBytes(URLs.RepeatImage(repeat), Token);

            bool res;
            if (res = CheckResults("", true))
                Repeatimage = result;

            return res;
        }
        public async Task<bool> GetCameraConfig()
        {
            Logger.Info("GET: {url}", URLs.Configuration_Camera());

            string result = await Connection.Get(URLs.Configuration_Camera(), Token);

            bool res;
            if (res = CheckResults(result))
                ConfigurationCamera = JsonConvert.DeserializeObject<V275_Configuration_Camera>(result);

            return res;
        }

        public async Task<bool> GetIsRunReady()
        {
            Logger.Info("GET: {url}", URLs.IsRunReady());

            string result = await Connection.Get(URLs.IsRunReady(), Token);

            bool res;
            if (res = CheckResults(result))
            {
                if (result == "OK")
                    return res;
                else
                    return false;
            }
            return res;
        }
        public async Task<bool> RunJob(string jobName)
        {
            Logger.Info("PUT: {url}", URLs.RunJob(jobName));

            await Connection.Put(URLs.RunJob(jobName), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> StartJob()
        {
            Logger.Info("PUT: {url}", URLs.StartJob());

            await Connection.Put(URLs.StartJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> StopJob()
        {
            Logger.Info("PUT: {url}", URLs.StopJob());

            await Connection.Put(URLs.StopJob(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> PauseJob()
        {
            Logger.Info("PUT: {url}", URLs.PauseJob());

            await Connection.Put(URLs.PauseJob(), "", Token);

            return CheckResults("", true);
        }

        public async Task<bool> DeleteSector(string sectorName)
        {
            Logger.Info("DELETE: {url}", URLs.DeleteSector(sectorName));

            _ = await Connection.Delete(URLs.DeleteSector(sectorName), Token);

            return CheckResults("", true);
        }
        public async Task<bool> AddSector(string sectorName, string json)
        {
            Logger.Info("POST: {url}", URLs.AddSector(sectorName));

            _ = await Connection.Post(URLs.AddSector(sectorName), json, Token);

            return CheckResults("", true);
        }

        public async Task<bool> Inspect()
        {
            Logger.Info("PUT: {url}", URLs.Inspect());

            await Connection.Put(URLs.Inspect(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> Detect()
        {
            Logger.Info("PUT: {url}", URLs.Detect());

            await Connection.Put(URLs.Detect(), "", Token);

            return CheckResults("", true);
        }
        public async Task<bool> GetDetect()
        {
            Logger.Info("GET: {url}", URLs.Detect());

            string result = await Connection.Get(URLs.Detect(), Token);

            bool res;
            if (res = CheckResults(result))
                Detected = JsonConvert.DeserializeObject<V275_DetectResponse>(result);

            return res;
        }

        public async Task<bool> Print(bool start)
        {
            Logger.Info("PUT: {url}", URLs.Print());

            await Connection.Put(URLs.Print(), URLs.Print_Body(start), Token);

            return CheckResults("", true);
        }

        public async Task<bool> SetRepeat(int repeat)
        {
            Logger.Info("PUT: {url}", URLs.History(repeat.ToString()));

            await Connection.Put(URLs.History(repeat.ToString()), "", Token);

            return CheckResults("", true);
        }


    }
}

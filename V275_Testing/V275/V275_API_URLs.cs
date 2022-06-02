using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275
{
    public class V275_API_URLs
    {
        public string Host { get; set; }
        public uint SystemPort { get; set; }
        public uint NodeNumber { get; set; }
        private string NodePort => $"{SystemPort + NodeNumber}";

        /// <summary>
        /// Events
        /// </summary>
        public string WS_NodeEvents => $"ws://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/events";
        public string WS_SystemEvents => $"ws://{Host}:{SystemPort}/api/printinspection/event";

        /// <summary>
        /// System API
        /// </summary>
        private string SystemBase => $"http://{Host}:{SystemPort}/api/printinspection";
        public string Devices() => $"{SystemBase}/devices";
        public string Product() => $"{SystemBase}/product";

        /// <summary>
        /// Node API
        /// </summary>
        private string NodeBase => $"http://{Host}:{NodePort}/api/printinspection";
        public string Login(bool monitor = false, bool temporary = false) => $"{NodeBase}/{NodeNumber}/security/login?monitor={(monitor ? "1" : "0")}&temporary={(temporary ? "1" : "0")}";
        public string Logout() => $"{NodeBase}/{NodeNumber}/security/logout";

        public string GradingStandards() => $"{NodeBase}/{NodeNumber}/gradingstandards";

        public string Job() => $"{NodeBase}/{NodeNumber}/inspection/job";
        public string DeleteSector(string sectorName) => $"{NodeBase}/{NodeNumber}/inspection/job/sectors/{sectorName}";
        public string AddSector(string sectorName) => $"{NodeBase}/{NodeNumber}/inspection/job/sectors/{sectorName}";

        public string Print() => $"{NodeBase}/{NodeNumber}/inspection/print";
        public string Print_Body(bool enabled) => $"{{\"enabled\":{(enabled ? "true" : "false")}}}";

        public string History(string repeatNumber) => $"{NodeBase}/{NodeNumber}/inspection/setup/image?source=history&repeat={repeatNumber}";
        public string History() => $"{NodeBase}/{NodeNumber}/inspection/setup/image/history";

        public string Available() => $"{NodeBase}/{NodeNumber}/inspection/setup/image/available";

        public string Inspect() => $"{NodeBase}/{NodeNumber}/inspection/setup/inspect";
        public string Report() => $"{NodeBase}/{NodeNumber}/inspection/setup/report";
        public string Detect() => $"{NodeBase}/{NodeNumber}/inspection/setup/detect";

        public string RepeatImage(int repeatNumber) => $"{NodeBase}/{NodeNumber}/inspection/repeat/images/{repeatNumber}?scale=1.0";

        public string VerifySymbologies() => $"{NodeBase}/{NodeNumber}/inspection/verify/symbologies";

        public string Configuration_Camera() => $"{NodeBase}/{NodeNumber}/configuration/camera";


    }
}

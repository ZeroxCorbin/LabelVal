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
        public string SystemPort { get; set; }
        public string NodeNumber { get; set; }
        private string NodePort => $"808{NodeNumber}";

        //public void Configure(string host, string systemPort, string nodeNumber = "1") { Host = host; SystemPort = systemPort; NodeNumber = nodeNumber; }


        public string Login(bool monitor = false, bool temporary = false) => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/security/login?monitor={(monitor ? "1" : "0")}&temporary={(temporary ? "1" : "0")}";
        public string Logout() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/security/logout";

        public string Devices() => $"http://{Host}:{SystemPort}/api/printinspection/devices";
public string Product() => $"http://{Host}:{SystemPort}/api/printinspection/product";

        public string WS_NodeEvents => $"ws://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/events";
        public string WS_SystemEvents => $"ws://{Host}:{SystemPort}/api/printinspection/event";

        public string GradingStandards() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/gradingstandards";

        public string Job() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/job";
        public string DeleteSector(string sectorName) => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/job/sectors/{sectorName}";
        public string AddSector(string sectorName) => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/job/sectors/{sectorName}";

        public string Print() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/print";
        public string Print_Body(bool enabled) => $"{{\"enabled\":{(enabled ? "true" : "false")}}}";

        public string History(string repeatNumber) => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/setup/image?source=history&repeat={repeatNumber}";
        public string History() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/setup/image/history";

        public string Available () => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/setup/image/available";

        public string Inspect() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/setup/inspect";
        public string Report() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/setup/report";
        public string Detect() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/setup/detect";

        public string RepeatImage(int repeatNumber) => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/repeat/images/{repeatNumber}?scale=1.0";

        public string VerifySymbologies() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/inspection/verify/symbologies";

        public string Configuration_Camera() => $"http://{Host}:{NodePort}/api/printinspection/{NodeNumber}/configuration/camera";

        public string CameraCommand(string nodeNumber) => "";

    }
}

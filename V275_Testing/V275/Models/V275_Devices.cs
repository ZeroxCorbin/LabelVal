using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V275_Testing.V275.Models
{
    public class V275_Devices
    {
        public Node[] nodes { get; set; }
        public Camera[] cameras { get; set; }

        public class Node
        {
            public int enumeration { get; set; }
            public string cameraMAC { get; set; }
            public string ipAddress { get; set; }
            public int port { get; set; }
            public bool connected { get; set; }
            public string managerStatus { get; set; }
            public string printerModel { get; set; }
            public int packetLimiting { get; set; }
        }

        public class Camera
        {
            public string type { get; set; }
            public string gateway { get; set; }
            public string mac { get; set; }
            public string ip { get; set; }
            public string subnet { get; set; }
            public string available { get; set; }
        }

    }
}

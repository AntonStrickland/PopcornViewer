using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace PopcornViewer
{
    public static class GlobalVars
    {
        //Chat variables
        public static Socket socket;
        public static EndPoint endpointLocal;
        public static EndPoint endpointRemote;
        public static string localIP { get; set; }
        public static byte[] ChatBuffer = new byte[1500];
    }
}

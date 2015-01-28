using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Text.RegularExpressions;
using Google.YouTube;

namespace PopcornViewer
{
    public partial class ChatCredentials : Form
    {
        
        

        public ChatCredentials()
        {
            
            InitializeComponent();
        }

        private void ChatConnectButton_Click(object sender, EventArgs e)
        {
            MainForm mainForm = new MainForm();
            GlobalVars.ChatBuffer = new byte[1500];
            //binding socket
            GlobalVars.endpointLocal = new IPEndPoint(IPAddress.Parse(GlobalVars.localIP), Convert.ToInt32(PortTextbox.Text));
            GlobalVars.socket.Bind(GlobalVars.endpointLocal);

            //Connecting to remote IP
            GlobalVars.endpointRemote = new IPEndPoint(IPAddress.Parse(remoteIpTextbox.Text), Convert.ToInt32(PortTextbox.Text));
            GlobalVars.socket.Connect(GlobalVars.endpointRemote);

            //Listening to specific port
            GlobalVars.socket.BeginReceiveFrom(GlobalVars.ChatBuffer, 0, GlobalVars.ChatBuffer.Length, SocketFlags.None, ref GlobalVars.endpointRemote, new AsyncCallback(mainForm.MessageCallBack), GlobalVars.ChatBuffer);
            this.Close();

            
        }

        
    }
}

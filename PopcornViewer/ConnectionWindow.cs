using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace PopcornViewer
{
    public partial class ConnectionWindow : Form
    {
        // Local Variables
        MainForm Parent;

        public ConnectionWindow(MainForm MF)
        {
            InitializeComponent();

            Parent = MF;

            // Display Local IP Address
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                }
            }
            IPAddressBox.Text = localIP;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddNetwork AddNet = new AddNetwork(this, false);
            AddNet.ShowDialog();
        }

        private void NetworkList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Disallow single object reference function if none are selected
            if (NetworkList.SelectedIndices.Count == 1)
            {
                EditButton.Enabled = true;
                RemoveButton.Enabled = true;
                ConnectButton.Enabled = true;
            }
            else
            {
                EditButton.Enabled = false;
                RemoveButton.Enabled = false;
                ConnectButton.Enabled = false;
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            AddNetwork AddNet = new AddNetwork(this, true);
            AddNet.ShowDialog();
        }

        private void NetworkList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Delete)
            {
                if (NetworkList.SelectedIndices.Count == 1)
                {
                    NetworkList.Items.RemoveAt(NetworkList.SelectedIndices[0]);
                }
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (NetworkList.SelectedIndices.Count == 1)
            {
                NetworkList.Items.RemoveAt(NetworkList.SelectedIndices[0]);
            }
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (NicknameBox.Text.Length > 0)
            {
                // Try to connect to the server
                Parent.SelfSocket = new TcpClient();
                Parent.Chat("Connecting to " + NetworkList.Items[NetworkList.SelectedIndices[0]].SubItems[0].Text + "...", "CONSOLE");
                try { Parent.SelfSocket.Connect(NetworkList.Items[NetworkList.SelectedIndices[0]].SubItems[1].Text, Convert.ToInt32(NetworkList.Items[NetworkList.SelectedIndices[0]].SubItems[2].Text)); }
                catch (Exception Ex)
                {
                    Parent.Chat(Ex.ToString(), "CONSOLE");
                }
                if (!Parent.SelfSocket.Connected)
                {
                    Parent.Chat("Unable to connect to " + IPAddressBox.Text, "CONSOLE");
                    this.Close();
                    return;
                }

                Parent.SelfStream = Parent.SelfSocket.GetStream();
                byte[] BytesOut = Encoding.ASCII.GetBytes(NicknameBox.Text + "$");
                Parent.SelfStream.Write(BytesOut, 0, BytesOut.Length);
                Parent.SelfStream.Flush();

                Parent.clListener = new BackgroundWorker();
                Parent.clListener.WorkerSupportsCancellation = true;
                Parent.clListener.DoWork += new DoWorkEventHandler(Parent.GetMessage);
                Parent.clListener.RunWorkerAsync();

                this.Close();
            }
            else MessageBox.Show("Please enter a Nickname.", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void HostButton_Click(object sender, EventArgs e)
        {
            if (NicknameBox.Text.Length > 0)
            {
                // Initate server on seperate thread
                Parent.HostPort = (int)PortBox.Value;
                Parent.bwListener = new BackgroundWorker();
                Parent.bwListener.WorkerSupportsCancellation = true;
                Parent.bwListener.DoWork += new DoWorkEventHandler(Parent.Listen);
                Parent.bwListener.RunWorkerAsync();
                
                this.Close();
            }
            else MessageBox.Show("Please enter a Nickname.", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

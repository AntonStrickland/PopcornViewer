using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopcornViewer
{
    public partial class ConnectionWindow : Form
    {
        public ConnectionWindow()
        {
            InitializeComponent();

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
                // Connect to server
            }
            else MessageBox.Show("Please enter a Nickname.", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void HostButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

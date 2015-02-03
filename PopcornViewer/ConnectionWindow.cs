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
using System.IO;

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
                    break;
                }
            }
            IPAddressBox.Text = localIP;
        }

        // Opens up the AddNetwork window to add networks to the list
        private void AddButton_Click(object sender, EventArgs e)
        {
            AddNetwork AddNet = new AddNetwork(this, false);
            AddNet.ShowDialog();
        }

        // Disables/enables buttons based on which network is selected
        private void NetworkList_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditButton.Enabled = (NetworkList.SelectedIndices.Count == 1);
            RemoveButton.Enabled = EditButton.Enabled;
            ConnectButton.Enabled = (EditButton.Enabled && !Parent.Hosting);
        }

        // Brings up the AddNetwork window to change the selected network
        private void EditButton_Click(object sender, EventArgs e)
        {
            AddNetwork AddNet = new AddNetwork(this, true);
            AddNet.ShowDialog();
        }

        // Removes networks from the network box
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

        // Removes networks from the network box
        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (NetworkList.SelectedIndices.Count == 1)
            {
                NetworkList.Items.RemoveAt(NetworkList.SelectedIndices[0]);
            }
        }

        // Begins connection and sends nickname to server
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (NicknameBox.Text.Length < 1)
            {
                MessageBox.Show("Please enter a Nickname.", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            Parent.NicknameLabel.Text = NicknameBox.Text;
            // Try to connect to the server
            Parent.SelfSocket = new TcpClient();
            Parent.Chat("Connecting to " + NetworkList.Items[NetworkList.SelectedIndices[0]].SubItems[0].Text + "...", "CONSOLE");
            try { Parent.SelfSocket.Connect(NetworkList.Items[NetworkList.SelectedIndices[0]].SubItems[1].Text, Convert.ToInt32(NetworkList.Items[NetworkList.SelectedIndices[0]].SubItems[2].Text)); }
            catch
            {
                Parent.Chat("Unable to connect to " + IPAddressBox.Text, "CONSOLE");
                this.Close();
                return;
            }

            Parent.SelfStream = Parent.SelfSocket.GetStream();
            byte[] BytesOut = Encoding.UTF8.GetBytes(MainForm.Encrypt(NicknameBox.Text) + "$");
            Parent.SelfStream.Write(BytesOut, 0, BytesOut.Length);
            Parent.SelfStream.Flush();

            Parent.clListener = new BackgroundWorker();
            Parent.clListener.WorkerSupportsCancellation = true;
            Parent.clListener.DoWork += new DoWorkEventHandler(Parent.GetMessage);
            Parent.clListener.RunWorkerAsync();

            this.Close();
        }

        // Starts host thread and begins connection to loaclhost
        private void HostButton_Click(object sender, EventArgs e)
        {
            if (NicknameBox.Text.Length < 1)
            {
                MessageBox.Show("Please enter a Nickname.", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Initate server on seperate thread
            Parent.HostPort = (int)PortBox.Value;
            Parent.bwListener = new BackgroundWorker();
            Parent.bwListener.WorkerSupportsCancellation = true;
            Parent.bwListener.DoWork += new DoWorkEventHandler(Parent.Listen);
            Parent.bwListener.RunWorkerAsync();

            Parent.NicknameLabel.Text = NicknameBox.Text;
            // Try to connect to the server
            Thread ConnectionThread = new Thread(() => HostConnect());
            ConnectionThread.Start();
            this.Close();
        }

        // Allows host to connect to his own server
        private void HostConnect()
        {
            Parent.SelfSocket = new TcpClient();
            Parent.Chat("Connecting to localhost...", "CONSOLE");
            while (!Parent.SelfSocket.Connected && Parent.bwListener.IsBusy)
            {
                Parent.SelfSocket.Connect("localhost", Parent.HostPort);
            }
            Parent.SelfStream = Parent.SelfSocket.GetStream();
            byte[] BytesOut = Encoding.UTF8.GetBytes(MainForm.Encrypt(NicknameBox.Text) + "$");
            Parent.SelfStream.Write(BytesOut, 0, BytesOut.Length);
            Parent.SelfStream.Flush();

            Parent.clListener = new BackgroundWorker();
            Parent.clListener.WorkerSupportsCancellation = true;
            Parent.clListener.DoWork += new DoWorkEventHandler(Parent.GetMessage);
            Parent.clListener.RunWorkerAsync();
        }

        // Disables/enables buttons based on currently connceted status
        private void ConnectionWindow_Load(object sender, EventArgs e)
        {
            HostButton.Enabled = !Parent.Hosting;
            string PathName = @"networks.conf";
            try
            {
                using (StreamReader readFile = new StreamReader(PathName))
                {
                    //read in user data
                    String line;
                    NicknameBox.Text = (line = readFile.ReadLine());
                    PortBox.Text = (line = readFile.ReadLine());

                    //read in connection information from file
                    while ((line = readFile.ReadLine()) != null)
                    {
                        ListViewItem NewConnection = new ListViewItem(line);
                        NewConnection.SubItems.Add(line = readFile.ReadLine());
                        NewConnection.SubItems.Add(line = readFile.ReadLine());
                        NetworkList.Items.Add(NewConnection);
                    }
                }
            }
            catch { }
        }

        // Save network connection information and user information on closing
        private void ConnectionWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            string PathName = @"networks.conf";

            //write over old file
            FileStream fs = new FileStream(PathName, FileMode.Create, FileAccess.Write);
            fs.Close();

            //write user information to file
            StreamWriter writeText = new StreamWriter(PathName);
            writeText.WriteLine(NicknameBox.Text);
            writeText.WriteLine(PortBox.Text);

            //write connection information to file
            foreach(ListViewItem i in NetworkList.Items)
            {
                writeText.WriteLine(i.SubItems[0].Text);
                writeText.WriteLine(i.SubItems[1].Text);
                writeText.WriteLine(i.SubItems[2].Text);
            }
            
            
            writeText.Close();
            return;
        }

        // Removes all but alphanumerics
        private void NicknameBox_TextChanged(object sender, EventArgs e)
        {
            NicknameBox.Text = NicknameBox.Text.Replace(' ', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('/', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('\\', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace(':', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('*', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('\"', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('\'', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('%', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('<', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('>', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace('?', '\0');
            NicknameBox.Text = NicknameBox.Text.Replace(';', '\0');
            NicknameBox.Select(NicknameBox.Text.Length, 0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PopcornViewer
{
    public partial class SettingsWindow : Form
    {
        public SettingsWindow(MainForm MF)
        {
            InitializeComponent();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //Loads preferences on open
        private void Form1_Load(object sender, EventArgs e)
        {
            string PathName = @"ProgramSettings.conf";
            try
            {
                using (StreamReader readFile = new StreamReader(PathName))
                {
                    //read in user data
                    String line;
                    ListChooser.SelectedIndex = Convert.ToInt32(line = readFile.ReadLine());
                    SaveFilePath.Text = (line = readFile.ReadLine());

                    //read in connection information from file
                    while ((line = readFile.ReadLine()) != null)
                    {
                        ListViewItem NewConnection = new ListViewItem(line);
                        NewConnection.SubItems.Add(line = readFile.ReadLine());
                        IPAddressList.Items.Add(NewConnection);
                    }
                }
            }
            catch { }
        }

        // Disables/enables buttons based on which network is selected
        private void IPAddressList_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditButton.Enabled = (IPAddressList.SelectedIndices.Count == 1);
            RemoveButton.Enabled = EditButton.Enabled;
        }

        //Save File Path Location
        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.SaveFilePath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void ListChooser_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        //Action on Remove Button Click
        private void button3_Click(object sender, EventArgs e)
        {
            if (IPAddressList.SelectedIndices.Count == 1)
            {
                IPAddressList.Items.RemoveAt(IPAddressList.SelectedIndices[0]);
            }
        }

        //Action on Edit Button Click
        private void button2_Click(object sender, EventArgs e)
        {
            AddIPToList AddNet = new AddIPToList(this, true);
            AddNet.ShowDialog();
        }

        //Save preferences settings on close
        private void SettingsWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            string PathName = @"ProgramSettings.conf";

            // Write over old file
            FileStream fs = new FileStream(PathName, FileMode.Create, FileAccess.Write);
            fs.Close();

            // Write user information to file
            StreamWriter writeText = new StreamWriter(PathName);
            writeText.WriteLine(ListChooser.SelectedIndex);
            writeText.WriteLine(SaveFilePath.Text);

            // Write connection information to file
            foreach (ListViewItem i in IPAddressList.Items)
            {
                writeText.WriteLine(i.SubItems[0].Text);
                writeText.WriteLine(i.SubItems[1].Text);
            }

            writeText.Close();
            return;
        }

        //Action on Add Button Click
        private void AddButton_Click_1(object sender, EventArgs e)
        {
            AddIPToList AddNet = new AddIPToList(this, false);
            AddNet.ShowDialog();
        }
    }
}

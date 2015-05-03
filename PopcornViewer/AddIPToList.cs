using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PopcornViewer
{
    public partial class AddIPToList : Form
    {
        // Local Variables
        SettingsWindow Parent;
        bool EditMode;

        public AddIPToList(SettingsWindow CW, bool EM)
        {
            InitializeComponent();
            Parent = CW;
            EditMode = EM;

            if (EM)
            {
                NameBox.Text = Parent.IPAddressList.SelectedItems[0].SubItems[0].Text;
                IPAddressBox.Text = Parent.IPAddressList.SelectedItems[0].SubItems[1].Text;
            }
        }

        // Closes the window
        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Saves the network and closes the window
        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Add the network to the listview
            if (NameBox.Text.Length > 0 && IPAddressBox.Text.Length > 0)
            {
                ListViewItem NewConnection = new ListViewItem(NameBox.Text);
                NewConnection.SubItems.Add(IPAddressBox.Text);

                if (!EditMode)
                {
                    Parent.IPAddressList.Items.Add(NewConnection);
                }
                else Parent.IPAddressList.Items[Parent.IPAddressList.SelectedIndices[0]] = NewConnection;
            }
            else MessageBox.Show("Address or name unspecified!", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private void IPAddressBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void NameBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}

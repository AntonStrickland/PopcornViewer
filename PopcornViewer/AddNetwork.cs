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
    public partial class AddNetwork : Form
    {
        // Local Variables
        ConnectionWindow Parent;
        bool EditMode;

        public AddNetwork(ConnectionWindow CW, bool EM)
        {
            InitializeComponent();
            Parent = CW;
            EditMode = EM;

            if (EM)
            {
                NameBox.Text = Parent.NetworkList.SelectedItems[0].SubItems[0].Text;
                IPAddressBox.Text = Parent.NetworkList.SelectedItems[0].SubItems[1].Text;
                PortBox.Value = Convert.ToInt32(Parent.NetworkList.SelectedItems[0].SubItems[2].Text);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Add the network to the listview
            if (NameBox.Text.Length > 0 && IPAddressBox.Text.Length > 0)
            {
                ListViewItem NewConnection = new ListViewItem(NameBox.Text);
                NewConnection.SubItems.Add(IPAddressBox.Text);
                NewConnection.SubItems.Add(PortBox.Value.ToString());

                if (!EditMode)
                {
                    Parent.NetworkList.Items.Add(NewConnection);
                }
                else Parent.NetworkList.Items[Parent.NetworkList.SelectedIndices[0]] = NewConnection;
            }
            else MessageBox.Show("Address or name unspecified!", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }
    }
}

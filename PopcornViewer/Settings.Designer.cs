namespace PopcornViewer
{
    partial class SettingsWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Browse = new System.Windows.Forms.Button();
            this.IPAddressList = new System.Windows.Forms.ListView();
            this.ColName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.IPAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SaveFilePath = new System.Windows.Forms.TextBox();
            this.ListChooser = new System.Windows.Forms.ComboBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.EditButton = new System.Windows.Forms.Button();
            this.AddButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // Browse
            // 
            this.Browse.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.Browse.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.Browse.Location = new System.Drawing.Point(193, 18);
            this.Browse.Name = "Browse";
            this.Browse.Size = new System.Drawing.Size(68, 19);
            this.Browse.TabIndex = 0;
            this.Browse.Text = "Browse";
            this.Browse.UseVisualStyleBackColor = true;
            this.Browse.Click += new System.EventHandler(this.button1_Click);
            // 
            // IPAddressList
            // 
            this.IPAddressList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IPAddressList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColName,
            this.IPAddress});
            this.IPAddressList.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.IPAddressList.FullRowSelect = true;
            this.IPAddressList.MultiSelect = false;
            this.IPAddressList.GridLines = true;
            this.IPAddressList.Location = new System.Drawing.Point(6, 48);
            this.IPAddressList.Name = "IPAddressList";
            this.IPAddressList.Size = new System.Drawing.Size(254, 214);
            this.IPAddressList.TabIndex = 2;
            this.IPAddressList.UseCompatibleStateImageBehavior = false;
            this.IPAddressList.View = System.Windows.Forms.View.Details;
            this.IPAddressList.SelectedIndexChanged += new System.EventHandler(this.IPAddressList_SelectedIndexChanged);
            this.IPAddressList.Leave += new System.EventHandler(this.IPAddressList_SelectedIndexChanged);
            // 
            // ColName
            // 
            this.ColName.Text = "Name";
            this.ColName.Width = 75;
            // 
            // IPAddress
            // 
            this.IPAddress.Text = "IP Address";
            this.IPAddress.Width = 175;
            // 
            // SaveFilePath
            // 
            this.SaveFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveFilePath.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.SaveFilePath.Location = new System.Drawing.Point(6, 18);
            this.SaveFilePath.Name = "SaveFilePath";
            this.SaveFilePath.Size = new System.Drawing.Size(181, 19);
            this.SaveFilePath.TabIndex = 3;
            this.SaveFilePath.Text = "C:/User/Videos";
            // 
            // ListChooser
            // 
            this.ListChooser.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.ListChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ListChooser.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.ListChooser.FormattingEnabled = true;
            this.ListChooser.Items.AddRange(new object[] {
            "Blacklist",
            "Whitelist"});
            this.ListChooser.Location = new System.Drawing.Point(6, 18);
            this.ListChooser.Name = "ListChooser";
            this.ListChooser.Size = new System.Drawing.Size(121, 21);
            this.ListChooser.TabIndex = 9;
            this.ListChooser.SelectedIndexChanged += new System.EventHandler(this.ListChooser_SelectedIndexChanged);
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.SelectedPath = "C:\\Users\\Chris\\Videos";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.SaveFilePath);
            this.groupBox1.Controls.Add(this.Browse);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.groupBox1.Location = new System.Drawing.Point(13, 325);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(271, 47);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Local File Options";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.RemoveButton);
            this.groupBox2.Controls.Add(this.EditButton);
            this.groupBox2.Controls.Add(this.AddButton);
            this.groupBox2.Controls.Add(this.IPAddressList);
            this.groupBox2.Controls.Add(this.ListChooser);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.groupBox2.Location = new System.Drawing.Point(13, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(270, 306);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Network Management";
            // 
            // RemoveButton
            // 
            this.RemoveButton.Enabled = false;
            this.RemoveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RemoveButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.RemoveButton.Location = new System.Drawing.Point(154, 266);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(68, 23);
            this.RemoveButton.TabIndex = 12;
            this.RemoveButton.Text = "Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.button3_Click);
            // 
            // EditButton
            // 
            this.EditButton.Enabled = false;
            this.EditButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.EditButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.EditButton.Location = new System.Drawing.Point(80, 266);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(68, 23);
            this.EditButton.TabIndex = 11;
            this.EditButton.Text = "Edit";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.button2_Click);
            // 
            // AddButton
            // 
            this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F);
            this.AddButton.Location = new System.Drawing.Point(6, 266);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(68, 23);
            this.AddButton.TabIndex = 10;
            this.AddButton.Text = "Add";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click_1);
            // 
            // SettingsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(296, 387);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "SettingsWindow";
            this.Text = "Preferences";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsWindow_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Browse;
        public System.Windows.Forms.ListView IPAddressList;
        private System.Windows.Forms.ColumnHeader ColName;
        private System.Windows.Forms.ColumnHeader IPAddress;
        private System.Windows.Forms.TextBox SaveFilePath;
        private System.Windows.Forms.ComboBox ListChooser;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Button EditButton;
        private System.Windows.Forms.Button AddButton;
    }
}


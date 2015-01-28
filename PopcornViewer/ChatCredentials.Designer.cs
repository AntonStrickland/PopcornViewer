namespace PopcornViewer
{
    partial class ChatCredentials
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
            this.remoteIpTextbox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PortTextbox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ChatConnectButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // remoteIpTextbox
            // 
            this.remoteIpTextbox.Location = new System.Drawing.Point(113, 38);
            this.remoteIpTextbox.Name = "remoteIpTextbox";
            this.remoteIpTextbox.Size = new System.Drawing.Size(100, 20);
            this.remoteIpTextbox.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "RemoteIP:";
            // 
            // PortTextbox
            // 
            this.PortTextbox.Location = new System.Drawing.Point(113, 89);
            this.PortTextbox.Name = "PortTextbox";
            this.PortTextbox.Size = new System.Drawing.Size(100, 20);
            this.PortTextbox.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 92);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port:";
            // 
            // ChatConnectButton
            // 
            this.ChatConnectButton.Location = new System.Drawing.Point(82, 161);
            this.ChatConnectButton.Name = "ChatConnectButton";
            this.ChatConnectButton.Size = new System.Drawing.Size(75, 23);
            this.ChatConnectButton.TabIndex = 4;
            this.ChatConnectButton.Text = "Connect";
            this.ChatConnectButton.UseVisualStyleBackColor = true;
            this.ChatConnectButton.Click += new System.EventHandler(this.ChatConnectButton_Click);
            // 
            // ChatCredentials
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.ChatConnectButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.PortTextbox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.remoteIpTextbox);
            this.Name = "ChatCredentials";
            this.Text = "ChatCredentials";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox remoteIpTextbox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox PortTextbox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button ChatConnectButton;
    }
}
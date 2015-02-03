using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Collections;
using System.Text.RegularExpressions;
using Google.YouTube;

namespace PopcornViewer
{
    public partial class MainForm : Form
    {
        // Form Variables
        const string DEV_STRING = "AI39si4LgRzD-nVk4ZIHLC5pLti7cBcVLKKhJIS7PCyosewQMlAVgSqtCKMfzTTLwScr4qV6UxeDFo7YsfjBaEdLn3lVJocjbA";
        List<string> PlaylistURLs = new List<string>();

        // Index in PlaylistURLs of currently playing video
        int CurrentlyPlaying = -1;

        // Playlist Drag/Drop Variables
        bool PlaylistDragging = false;
        int SavedX = 0;
        int SavedY = 0;
        const int TOLERANCE = 2;

        // Socket Objects
        public bool Hosting = false;
        public int HostPort;
        public static Hashtable ClientsList;
        public BackgroundWorker bwListener;
        public TcpClient SelfSocket;
        public NetworkStream SelfStream;
        public BackgroundWorker clListener;
        TcpListener ServerSocket;
        TcpClient ClientSocket;
        List<Thread> ChatClient2Threads = new List<Thread>();

        // Main function
        public MainForm()
        {
            InitializeComponent();

            Playlist.DrawMode = DrawMode.OwnerDrawFixed;
            Playlist.DrawItem += new DrawItemEventHandler(Playlist_DrawItem);
        }

        // Functions controling Playlist mouse and keyboard operations
        private void Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Delete)
            {
                if (Playlist.SelectedIndex >= 0)
                {
                    DeleteVideo(Playlist.SelectedIndex);
                }
            }
            else if (e.KeyData.Equals(Keys.Enter))
            {
                if (Playlist.SelectedIndex >= 0)
                {
                    PlayVideo(Playlist.SelectedIndex);
                }
            }
        }

        private void Playlist_DoubleClick(object sender, EventArgs e)
        {   
            if (Playlist.SelectedIndex >= 0)
            {
                PlayVideo(Playlist.SelectedIndex);
            }
        }

        // Enables and disables appropriate options on opening
        private void PlaylistContextMenu_Opening(object sender, CancelEventArgs e)
        {
            // Don't let user select a video specific option without a video selected
            if (Playlist.SelectedIndex >= 0)
            {
                copyPlaylistMenuItem.Enabled = true;
                deletePlaylistMenuItem.Enabled = true;
                playPlaylistMenuItem.Enabled = true;
            }
            else
            {
                copyPlaylistMenuItem.Enabled = false;
                deletePlaylistMenuItem.Enabled = false;
                playPlaylistMenuItem.Enabled = false;
            }
        }

        // These functions define behaviors for Playlist context menu items
        private void copyPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ConvertURLToBrowser(PlaylistURLs[Playlist.SelectedIndex]));
        }

        private void deletePlaylistMenuItem_Click(object sender, EventArgs e)
        {
            DeleteVideo(Playlist.SelectedIndex);
        }

        private void addVideoPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            if (AddToPlaylist(Clipboard.GetText())) ;
            else MessageBox.Show("Clipboard contents do not contain a valid Youtube URL!", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void playPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            CurrentlyPlaying = Playlist.SelectedIndex;
            if (Playlist.SelectedIndex >= 0)
            {
                PlayVideo(Playlist.SelectedIndex);
            }
        }

        // Handles logic for drag and drop in the Playlist
        private void Playlist_DragDrop(object sender, DragEventArgs e)
        {
            string url = (string)e.Data.GetData(DataFormats.Text, false);
            // Drag and drop to add Youtube Videos
            if (IsYoutubeURL(url))
            {
                // Not calling the usual add to playlist function to avoid
                // slow, redundant IsYoutubeURL calls.
                PlaylistURLs.Add(ConvertURLToEmbeded(url));
                Video video = RequestFromYoutube(url);
                Playlist.Items.Add(video.Title);
                UpdatePlaylistCount();
            }
            // Drag and drop to rearrange Playlist
            else if (PlaylistDragging)
            {
                Point point = Playlist.PointToClient(new Point(e.X, e.Y));
                int index = Playlist.IndexFromPoint(point);
                if (index < 0)
                {
                    index = Playlist.Items.Count - 1;
                }
                object data = e.Data.GetData(typeof(string));
                string ind = PlaylistURLs[Playlist.Items.IndexOf(data)];
                int beforeIndex = Playlist.Items.IndexOf(data);
                Playlist.Items.RemoveAt(beforeIndex);
                Playlist.Items.Insert(index, data);
                PlaylistURLs.Remove(ind);
                PlaylistURLs.Insert(index, ind);

                if (beforeIndex < CurrentlyPlaying && index > CurrentlyPlaying)
                {
                    CurrentlyPlaying--;
                }
                else if (beforeIndex == CurrentlyPlaying)
                {
                    CurrentlyPlaying = index;
                }

                Playlist.Refresh();
            }
        }

        private void Playlist_DragEnter(object sender, DragEventArgs e)
        {
            // Only allow text objects to drag/drop
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.All;
            }
            else e.Effect = DragDropEffects.None;
        }

        private void Playlist_MouseDown(object sender, MouseEventArgs e)
        {
            
            if (Playlist.SelectedIndex >= 0)
            {
                if (!PlaylistDragging)
                {
                    PlaylistDragging = true;
                    SavedX = e.Location.X;
                    SavedY = e.Location.Y;
                }
            }
        }

        private void Playlist_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void Playlist_MouseUp(object sender, MouseEventArgs e)
        {
            PlaylistDragging = false;
        }

        private void Playlist_MouseMove(object sender, MouseEventArgs e)
        {
            if (PlaylistDragging && Playlist.SelectedIndex >= 0)
            {
                if (SavedX > e.Location.X + TOLERANCE ||
                    SavedX < e.Location.X - TOLERANCE ||
                    SavedY < e.Location.Y - TOLERANCE ||
                    SavedY > e.Location.Y + TOLERANCE)
                {
                    Playlist.DoDragDrop(Playlist.SelectedItem, DragDropEffects.Move);
                }
            }
        }

        // Controls the edit Toolstrip Menu items' logic
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ConvertURLToBrowser(PlaylistURLs[Playlist.SelectedIndex]));
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (Playlist.SelectedIndex >= 0)
            {
                copyToolStripMenuItem.Enabled = true;
            }
            else copyToolStripMenuItem.Enabled = false;
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!AddToPlaylist(Clipboard.GetText()))
            {
                MessageBox.Show("Clipboard contents do not contain a valid Youtube URL!", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Deals with the playback toolstrip.
        private void repeatOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckPlaybackOptions();
            repeatOneToolStripMenuItem.Checked = true;
        }

        private void repeatAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckPlaybackOptions();
            repeatAllToolStripMenuItem.Checked = true;
        }

        private void shuffleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckPlaybackOptions();
            shuffleToolStripMenuItem.Checked = true;
        }

        private void playNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckPlaybackOptions();
            playNextToolStripMenuItem.Checked = true;
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UncheckPlaybackOptions();
            pauseToolStripMenuItem.Checked = true;
        }

        // Deals with settings toolstrip
        private void hostingOptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectionWindow ConWin = new ConnectionWindow(this);
            ConWin.ShowDialog();
        }

        // Changes the Playlist item 0 to be bold showing that it is the one currently playing
        private void Playlist_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0)
            {
                if (e.Index == CurrentlyPlaying)
                {
                    e.Graphics.DrawString(Playlist.Items[e.Index].ToString(), new Font("Microsoft Sans Serif", 8, FontStyle.Bold), Brushes.Black, e.Bounds);
                }
                else e.Graphics.DrawString(Playlist.Items[e.Index].ToString(), e.Font, Brushes.Black, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        // Handles the logic of the chatbox
        private void ChatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter && ChatBox.Text.Length > 0 && SelfSocket != null && SelfSocket.Connected)
            {
                byte[] Chat = Encoding.UTF8.GetBytes(Encrypt(ChatBox.Text) + "$");
                SelfStream.Write(Chat, 0, Chat.Length);
                SelfStream.Flush();
                ChatBox.Text = "";
            }
        }

        // Scroll chat when text changed
        private void ChatHistory_TextChanged(object sender, EventArgs e)
        {
            ChatHistory.SelectionStart = ChatHistory.Text.Length;
            ChatHistory.ScrollToCaret();
        }

        // Handles tasks when form closes
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (Hosting)
            {
                Broadcast("Server closing...", "CONSOLE", true);
            }

            if (ClientSocket != null && ClientSocket.Connected) ClientSocket.Close();
            if (ServerSocket != null) ServerSocket.Stop();

            foreach(Thread T in ChatClient2Threads)
            {
                try { T.Abort(); }
                catch { }
            }

            if (clListener != null) clListener.CancelAsync();
            if (bwListener != null) bwListener.CancelAsync();

            SelfStream.Close();
        }

        // Loads connection window immediately
        private void MainForm_Shown(object sender, EventArgs e)
        {
            ConnectionWindow ConWin = new ConnectionWindow(this);
            ConWin.ShowDialog();
        }

        // Exit toolstrip click
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

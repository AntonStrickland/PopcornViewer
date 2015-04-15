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
using System.IO;
using Google.Apis.YouTube.v3.Data;
using System.Diagnostics;


namespace PopcornViewer
{
    public partial class MainForm : Form
    {
        // Form Variables
        const string DEV_STRING = "AIzaSyCYSKxspULk1eESWMu2XZdvb3hH480snng";
        List<string> PlaylistURLs = new List<string>();
        bool Internal_Command = false;
        bool Seek_Immunity = false;
        public bool First_Connect = false;

        // Name of the current playlist file
        string CurrentPlaylistName = "";

        // Counters for voting
        int VoteCounter = 0;

        // Voting message box option yes/no holder
        DialogResult VoteResult = DialogResult.Ignore;

        // Timer for voting
        const int SECONDS_TO_VOTE = 20;
        int VoteTime = SECONDS_TO_VOTE;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer() { Interval = 1000 };
        
        // Number of votes allowed
        int NumOfVotes = 1;

        // Index in PlaylistURLs of currently playing video
        int CurrentlyPlaying = -1;

        // Playlist Drag/Drop Variables
        bool PlaylistDragging = false;
        int SavedX = 0;
        int SavedY = 0;
        const int TOLERANCE = 2;

        // Socket Objects
        public bool Connected = false;
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
                    PlayVideo(Playlist.SelectedIndex, false);
                }
            }
        }

        private void Playlist_DoubleClick(object sender, EventArgs e)
        {   
            if (Playlist.SelectedIndex >= 0)
            {
                PlayVideo(Playlist.SelectedIndex, false);
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
            if (!AddToPlaylist(Clipboard.GetText()))
                MessageBox.Show("Clipboard contents do not contain a valid Youtube URL!", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void playPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            CurrentlyPlaying = Playlist.SelectedIndex;
            if (Playlist.SelectedIndex >= 0)
            {
                PlayVideo(Playlist.SelectedIndex, false);
            }
        }

        // Handles logic for drag and drop in the Playlist
        private void Playlist_DragDrop(object sender, DragEventArgs e)
        {
            string url = (string)e.Data.GetData(DataFormats.Text, false);

            // Drag and drop to rearrange Playlist
            if (!AddToPlaylist(url) && PlaylistDragging)
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
                if (Hosting) BroadcastPlaylist();
                else BroadcastPlaylist("");
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
            if (Playlist.SelectedIndex >= 0 && !PlaylistDragging)
            {
                PlaylistDragging = true;
                SavedX = e.Location.X;
                SavedY = e.Location.Y;
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
            if (Playlist.SelectedIndex >= 0)
            {
                Clipboard.SetText(ConvertURLToBrowser(PlaylistURLs[Playlist.SelectedIndex]));
            } 
            else if (ChatBox.SelectedText != "")
            {
                Clipboard.SetText(ChatBox.SelectedText);
            }
            else if (ChatHistory.SelectedText != "")
            {
                Clipboard.SetText(ChatHistory.SelectedText);
            }
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            /*if (Playlist.SelectedIndex >= 0)
            {
                copyToolStripMenuItem.Enabled = true;
            }*/
            /*else if (ChatBox.Text.)
            {

            }
            else copyToolStripMenuItem.Enabled = false;*/
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChatBox.Text += Clipboard.GetText();
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
            ChatBox.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
            if (e.KeyData == Keys.Enter && ChatBox.Text.Length > 0 && SelfSocket != null && SelfSocket.Connected)
            {
                ClientBroadcast("MESSAGE$" + Encrypt(ChatBox.Text) + "$");
                ChatBox.Text = "";
            }
        }

        // Scroll chat when text changed
        private void ChatHistory_TextChanged(object sender, EventArgs e)
        {
            ChatHistory.Font = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
            ChatHistory.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
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
            else ClientBroadcast("");

            if (ClientSocket != null && ClientSocket.Connected) ClientSocket.Close();
            if (ServerSocket != null) ServerSocket.Stop();

            foreach(Thread T in ChatClient2Threads)
            {
                try { T.Abort(); }
                catch { }
            }

            if (clListener != null) clListener.CancelAsync();
            if (bwListener != null) bwListener.CancelAsync();

            if (SelfStream != null)
            {
                SelfStream.Close();
            }
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

        // Starts Voting
        private void startVoteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            beginVoteMenuItem.Enabled = false;
            if (Hosting) Broadcast("VOTETOSKIP", "", false);
            else
            {
                ClientBroadcast("VOTETOSKIP$");
            }
        }

        // Makes sure there are enough videos in the playlist to enable vote option
        private void startVoteToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            beginVoteMenuItem.Enabled = (Playlist.Items.Count >= 2 && CurrentlyPlaying != -1);
        }

        private void NicknameLabel_TextChanged(object sender, EventArgs e)
        {
            NicknameLabel.Width = TextRenderer.MeasureText(NicknameLabel.Text, NicknameLabel.Font).Width;
            if (NicknameLabel.Width > 65)
            {
                ChatBox.Width = ChatBox.Width - (NicknameLabel.Width - 58);
                ChatBox.Location = new Point(67 + (NicknameLabel.Width - 58), ChatBox.Location.Y);
            }
            else
            {
                ChatBox.Width = ChatBox.Width + (58 - NicknameLabel.Width);
                ChatBox.Location = new Point(67 - (58 - NicknameLabel.Width), ChatBox.Location.Y);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentPlaylistName != "")
            {
                using (StreamWriter sw = new StreamWriter(CurrentPlaylistName))
                {
                    for (int i = 0; i < PlaylistURLs.Count; i++)
                        sw.WriteLine(ConvertURLToBrowser(PlaylistURLs[i]));
                    sw.Close();
                }
            }
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PopcornViewer files (*.pop)|*.pop|txt files (*.txt)|*.txt";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CurrentPlaylistName = sfd.FileName;
                using (StreamWriter sw = new StreamWriter(CurrentPlaylistName))
                {
                    for (int i = 0; i < PlaylistURLs.Count; i++)
                        sw.WriteLine(ConvertURLToBrowser(PlaylistURLs[i]));
                    sw.Close();
                }
            }
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "PopcornViewer files (*.pop)|*.pop|txt files (*.txt)|*.txt";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                CurrentPlaylistName = ofd.FileName;
                Playlist.Items.Clear();
                PlaylistURLs.Clear();
                using (StreamReader sr = new StreamReader(ofd.FileName))
                {
                    while (!sr.EndOfStream)
                    {
                        string url = sr.ReadLine();
                        if (IsYoutubeURL(url))
                        {
                            PlaylistURLs.Add(ConvertURLToEmbeded(url));
                            Video video = RequestFromYoutube(url);
                            Playlist.Items.Add(video.Snippet.Title);
                        }
                    }
                    sr.Close();
                }
                UpdatePlaylistCount();
                if (Hosting)
                {
                    Broadcast("has opened a playlist", NicknameLabel.Text, false);
                    BroadcastPlaylist();
                }
                else BroadcastPlaylist("has opened a playlist");
            }
        }

        private void popcornHomepageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://web.mst.edu/~mabwgf/PopcornViewerWebsite/");
            }
            catch { }
        }

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ChatHistory.SelectedText);
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsWindow SetWin = new SettingsWindow(this);
            SetWin.ShowDialog();
        }

        private void transferFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Main TransferFileWin = new Main(ChatMembers, ClientsList);
            TransferFileWin.ShowDialog();
        }
    }
}

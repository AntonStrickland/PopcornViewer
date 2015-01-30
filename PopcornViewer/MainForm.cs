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
        #region Local Variables
        
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
        public int HostPort;
        public static Hashtable ClientsList;
        public BackgroundWorker bwListener;
        public TcpClient SelfSocket;
        public NetworkStream SelfStream;
        public BackgroundWorker clListener;
        TcpListener ServerSocket;
        TcpClient ClientSocket;
        List<Thread> ChatClient2Threads = new List<Thread>();

        #endregion

        #region Utility Functions

        // URL Conversion Tools
        private string ConvertURLToEmbeded(string url)
        {
            url = url.Replace("/watch?", "/");
            url = url.Replace('=', '/');
            url = url + "?version=3&enablejsapi=1";
            return url;
        }

        private string ConvertURLToBrowser(string url)
        {
            url = url.Replace("?version=3&enablejsapi=1", "");
            url = url.Replace("/v/", "/watch?v=");
            return url;
        }


        // Adds videos to Playlist given a browser URL
        private bool AddToPlaylist(string url)
        {
            if (IsYoutubeURL(url))
            {
                PlaylistURLs.Add(ConvertURLToEmbeded(url));
                Video video = RequestFromYoutube(url);
                Playlist.Items.Add(video.Title);
                UpdatePlaylistCount();
                return true;
            }
            else return false;
        }

        // Changes the text on the PlaylistLabel to reflect number of videos added
        private void UpdatePlaylistCount()
        {
            PlaylistLabel.Text = PlaylistLabel.Text.Substring(0, PlaylistLabel.Text.Length - 1);
            PlaylistLabel.Text = PlaylistLabel.Text + Playlist.Items.Count;
        }

        // Ensures that the given URL is actually for a Youtube video.

        private bool IsYoutubeURL(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "HEAD";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return (response.ResponseUri.ToString().Contains("youtube.com") &&
                            response.ResponseUri.ToString().Contains("watch?v=") &&
                            // Makes sure playlists are not able to be added since they are not videos
                            !response.ResponseUri.ToString().Contains("&list"));
                }
            }
            catch { return false; }
        }


        // Code for handling Youtube requests from a specific URL. Requires a Youtube link be verrified first.
        private Video RequestFromYoutube(string url)
        {
            string uri = url.Remove(0, url.IndexOf('=') + 1);

            YouTubeRequestSettings settings = new YouTubeRequestSettings("Popcorn Viewer", DEV_STRING);
            YouTubeRequest req = new YouTubeRequest(settings);

            Uri videoEntryUrl = new Uri("http://gdata.youtube.com/feeds/api/videos/" + uri);

            return req.Retrieve<Video>(videoEntryUrl);
        }


        // Performs bundled functions to play a video at URL index
        private void PlayVideo(int Index)
        {
            CurrentlyPlaying = Index;
            if (YoutubeVideo.Movie == null)
            {
                YoutubeVideo.Movie = PlaylistURLs[Index];
            }
            YoutubeVideo_CallFlash("loadVideoByUrl(" + PlaylistURLs[Index] + ")");
            YoutubeVideo_CallFlash("playVideo()");

            Playlist.Refresh();
        }

        private void DeleteVideo(int Index)
        {
            PlaylistURLs.RemoveAt(Index);
            Playlist.Items.RemoveAt(Index);
            UpdatePlaylistCount();

            if (Index == CurrentlyPlaying)
            {
                if (PlaylistURLs.Count <= Index && PlaylistURLs.Count != 0)
                {
                    PlayVideo(PlaylistURLs.Count - 1);
                }
                else if (PlaylistURLs.Count == 0)
                {
                    YoutubeVideo.Movie = null;
                }
                else PlayVideo(CurrentlyPlaying);
            }
            else if (Index < CurrentlyPlaying)
            {
                CurrentlyPlaying--;
                Playlist.Refresh();
            }
        }

        // Used to turn off all other checks in the playback toolstrip.
        private void UncheckPlaybackOptions()
        {
            repeatAllToolStripMenuItem.Checked = false;
            repeatOneToolStripMenuItem.Checked = false;
            shuffleToolStripMenuItem.Checked = false;
            playNextToolStripMenuItem.Checked = false;
            pauseToolStripMenuItem.Checked = false;
        }

        // Server host functions
        public void Listen(object sender, DoWorkEventArgs e)
        {
            Chat("Initiating chat service...", "CONSOLE");

            ServerSocket = new TcpListener(IPAddress.Any, HostPort);
            ClientSocket = default(TcpClient);
            bool Started = true;

            ClientsList = new Hashtable();

            try { ServerSocket.Start(); }
            catch (Exception Ex)
            {
                Chat(Ex.ToString(), "CONSOLE");
                Started = false;
            }

            while (Started)
            {
                try { ClientSocket = ServerSocket.AcceptTcpClient(); }
                catch { return; }

                byte[] BytesIn = new byte[65536];
                string DataFromClient;

                NetworkStream Stream = ClientSocket.GetStream();
                Stream.Read(BytesIn, 0, (int)ClientSocket.ReceiveBufferSize);
                DataFromClient = System.Text.Encoding.ASCII.GetString(BytesIn);
                DataFromClient = DataFromClient.Substring(0, DataFromClient.IndexOf("$"));

                ClientsList.Add(DataFromClient, ClientSocket);

                Broadcast("has joined the room", DataFromClient, false);

                Chat("has joined the room", DataFromClient);

                Thread ChatThread2 = new Thread(() => Speak(ClientSocket, DataFromClient));
                ChatClient2Threads.Add(ChatThread2);
                ChatThread2.Start();
            }

            ClientSocket.Close();
            ServerSocket.Stop();
            Chat("Terminating chat service...", "CONSOLE");
        }

        public static void Broadcast(string Message, string Entity, bool ClientFlag)
        {
            foreach (DictionaryEntry Item in ClientsList)
            {
                TcpClient BroadcastSocket = (TcpClient)Item.Value;
                NetworkStream BroadcastStream = BroadcastSocket.GetStream();
                Byte[] BroadcastBytes;

                if (ClientFlag)
                {
                    BroadcastBytes = Encoding.ASCII.GetBytes("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message );
                }
                else
                {
                    BroadcastBytes = Encoding.ASCII.GetBytes("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + " " + Message );
                }

                BroadcastStream.Write(BroadcastBytes, 0, BroadcastBytes.Length);
                BroadcastStream.Flush();
            }
        }

        private void Speak(TcpClient ClientSocket, string Entity)
        {
            byte[] BytesFrom = new byte[65536];
            string DataFromClient;

            while (true)
            {
                try
                {
                    NetworkStream Stream = ClientSocket.GetStream();
                    Stream.Read(BytesFrom, 0, (int)ClientSocket.ReceiveBufferSize);
                    DataFromClient = System.Text.Encoding.ASCII.GetString(BytesFrom);
                    DataFromClient = DataFromClient.Substring(0, DataFromClient.IndexOf("$"));
                    Chat(DataFromClient, Entity);

                    Broadcast(DataFromClient, Entity, true);
                }
                catch
                {
                    Chat("has left the room", Entity);
                    ClientsList.Remove(Entity);
                    Broadcast("has left the room", Entity, false);
                    return;
                }
                Thread.Sleep(200);
            }
        }

        public void GetMessage(object sender, DoWorkEventArgs e)
        {
            while (SelfSocket != null && SelfSocket.Connected)
            {
                SelfStream = SelfSocket.GetStream();
                byte[] InStream = new byte[65536];
                try { SelfStream.Read(InStream, 0, SelfSocket.ReceiveBufferSize); }
                catch { return; }
                ClientChat(Encoding.ASCII.GetString(InStream));
                Thread.Sleep(200);
            }
            Chat("Lost connection from server...", "CONSOLE");
        }

        // Sends chat to chatbox. Thread safe.
        public void Chat(string Message, string Entity)
        {
            if (InvokeRequired)
            {
                try { this.Invoke(new Action<string, string>(Chat), new object[] { Message, Entity }); }
                catch { }
                return;
            }
            if (ChatHistory.Text == "")
                ChatHistory.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message);
            else ChatHistory.AppendText("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message);
        }

        private void ClientChat(string Msg)
        {
            if (Msg != "")
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action<string>(ClientChat), new object[] { Msg });
                else
                    ChatHistory.AppendText(Msg);
            }
        }

        #endregion

        #region Flash-C# Communication

        // Handles the Flash -> C# communication
        private void YoutubeVideo_FlashCall(object sender, AxShockwaveFlashObjects._IShockwaveFlashEvents_FlashCallEvent e)
        {
            // Interpret the Command
            XmlDocument document = new XmlDocument();
            document.LoadXml(e.request);
            XmlAttributeCollection attributes = document.FirstChild.Attributes;
            string command = attributes.Item(0).InnerText;
            XmlNodeList list = document.GetElementsByTagName("arguments");
            List<string> listS = new List<string>();
            foreach (XmlNode l in list)
            {
                listS.Add(l.InnerText);
            }

            // Event Handler Flash -> C#
            switch (command)
            {
                // Initialize the Listeners
                case "onYouTubePlayerReady":
                    YoutubeVideo_CallFlash("addEventListener(\"onStateChange\",\"YTStateChange\")");
                    YoutubeVideo_CallFlash("addEventListener(\"onError\",\"YTError\")");
                    break;

                // On State Change
                case "YTStateChange":
                    switch (int.Parse(listS[0]))
                    {
                        // Not Started
                        case -1:
                            break;

                        // Ended
                        case 0:
                            // Repeat All
                            if (repeatAllToolStripMenuItem.Checked)
                            {
                                if (PlaylistURLs.Count - 1 > CurrentlyPlaying)
                                {
                                    PlayVideo(CurrentlyPlaying + 1);
                                }
                                else PlayVideo(0);
                            }

                            // Play Next
                            else if (playNextToolStripMenuItem.Checked)
                            {
                                if (PlaylistURLs.Count - 1 > CurrentlyPlaying)
                                {
                                    PlayVideo(CurrentlyPlaying + 1);
                                }
                            }

                            // Repeat One
                            else if (repeatOneToolStripMenuItem.Checked)
                            {
                                PlayVideo(CurrentlyPlaying);
                            }

                            // Shuffle
                            else if (shuffleToolStripMenuItem.Checked)
                            {
                                Random random = new Random();
                                int nextVideo = random.Next(0, PlaylistURLs.Count);

                                PlayVideo(nextVideo);
                            }
                            break;

                        // Playing
                        case 1:
                            break;

                        // Paused
                        case 2:
                            break;

                        // Buffering
                        case 3:
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }
        }

        // Calls functions in the Flash Player
        private string YoutubeVideo_CallFlash(string function)
        {
            string flashXMLrequest = "";
            string response = "";
            string flashFunction = "";
            List<string> flashFunctionArgs = new List<string>();

            Regex func2xml = new Regex(@"([a-z][a-z0-9]*)(\(([^)]*)\))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Match fmatch = func2xml.Match(function);

            // Bad Function Request String
            if (fmatch.Captures.Count != 1)
            {
                return "";
            }

            flashFunction = fmatch.Groups[1].Value.ToString();
            flashXMLrequest = "<invoke name=\"" + flashFunction + "\" returntype=\"xml\">";
            if (fmatch.Groups[3].Value.Length > 0)
            {
                flashFunctionArgs = parseDelimitedString(fmatch.Groups[3].Value);
                if (flashFunctionArgs.Count > 0)
                {
                    flashXMLrequest += "<arguments><string>";
                    flashXMLrequest += string.Join("</string><string>", flashFunctionArgs);
                    flashXMLrequest += "</string></arguments>";
                }
            }
            flashXMLrequest += "</invoke>";

            try
            {
                response = YoutubeVideo.CallFunction(flashXMLrequest);
            }
            catch { }

            return response;
        }

        // Creates flash args
        private static List<string> parseDelimitedString(string arguments, char delim = ',')
        {
            bool inQuotes = false;
            bool inNonQuotes = false;
            int whiteSpaceCount = 0;

            List<string> strings = new List<string>();

            StringBuilder sb = new StringBuilder();
            foreach (char c in arguments)
            {
                if (c == '\'' || c == '"')
                {
                    if (!inQuotes)
                        inQuotes = true;
                    else
                        inQuotes = false;

                    whiteSpaceCount = 0;
                }

                else if (c == delim)
                {
                    if (!inQuotes)
                    {
                        if (whiteSpaceCount > 0 && inQuotes)
                        {
                            sb.Remove(sb.Length - whiteSpaceCount, whiteSpaceCount);
                            inNonQuotes = false;
                        }
                        strings.Add(sb.Replace("'", string.Empty).Replace("\"", string.Empty).ToString());
                        sb.Remove(0, sb.Length);
                    }

                    else
                    {
                        sb.Append(c);
                    }
                    whiteSpaceCount = 0;
                }

                else if (char.IsWhiteSpace(c))
                {
                    if (inNonQuotes || inQuotes)
                    {
                        sb.Append(c);
                        whiteSpaceCount++;
                    }
                }

                else
                {
                    if (!inQuotes) inNonQuotes = true;
                    sb.Append(c);
                    whiteSpaceCount = 0;
                }
            }
            strings.Add(sb.Replace("'", string.Empty).Replace("\"", string.Empty).ToString());

            return strings;
        }

        #endregion

        #region Form Functions

        // Main function
        public MainForm()
        {
            InitializeComponent();

            ConnectionWindow ConWin = new ConnectionWindow(this);

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
            if (AddToPlaylist(Clipboard.GetText())) ;
            else MessageBox.Show("Clipboard contents do not contain a valid Youtube URL!", "Popcorn Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                byte[] Chat = Encoding.ASCII.GetBytes(ChatBox.Text + "$");
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

            if (ClientSocket != null && ClientSocket.Connected) ClientSocket.Close();
            if (ServerSocket != null) ServerSocket.Stop();

            foreach(Thread T in ChatClient2Threads)
            {
                try { T.Abort(); }
                catch { }
            }

            if (clListener != null) clListener.CancelAsync();
            if (bwListener != null) bwListener.CancelAsync();
        }

        #endregion
    }
}

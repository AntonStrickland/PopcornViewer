using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.IO;

namespace PopcornViewer
{
    public partial class MainForm
    {
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
                if (url.Contains("&"))
                {
                    url = url.Substring(0, url.IndexOf('&'));
                }
                PlaylistURLs.Add(ConvertURLToEmbeded(url));
                Video video = RequestFromYoutube(url);
                Playlist.Items.Add(video.Snippet.Title);
                UpdatePlaylistCount();
                if (Hosting)
                {
                    Broadcast("has added " + video.Snippet.Title + " to the playlist", NicknameLabel.Text, false);
                    BroadcastPlaylist();
                }
                else BroadcastPlaylist("has added " + video.Snippet.Title + " to the playlist");
                return true;
            }
            else return false;
        }

        // Changes the text on the PlaylistLabel to reflect number of videos added
        private void UpdatePlaylistCount()
        {
            PlaylistLabel.Text = "Playlist Count: " + Playlist.Items.Count;
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
                            response.ResponseUri.ToString().Contains("watch?v="));// &&
                            // Makes sure playlists are not able to be added since they are not videos
                            //!response.ResponseUri.ToString().Contains("play"));
                }
            }
            catch { return false; }
        }

        // Code for handling Youtube requests from a specific URL. Requires a Youtube link be verrified first.
        private Video RequestFromYoutube(string url)
        {
            string id = url.Remove(0, url.IndexOf('=') + 1);
            if (id.IndexOf('&') >= 0)
            {
                id = id.Remove(id.IndexOf('&'), id.Length - id.IndexOf('&'));
            }
            
            YouTubeService Service = new YouTubeService(new BaseClientService.Initializer(){ ApiKey = DEV_STRING, ApplicationName = "Youtube Synch" });
            var Request = Service.Videos.List("snippet");
            Request.Id = id;

            return Request.Execute().Items[0];
        }

        // Performs bundled functions to play a video at URL index. Now thread safe!
        // RecursiveFlag, when true, stops the broadcast. This should be raised to prevent loops. Keep false if not sure.
        private void PlayVideo(int Index, bool RecursiveFlag)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action<int, bool>(PlayVideo), new object[] { Index, RecursiveFlag }); }
                catch { return; }
            }
            else
            {
                CurrentlyPlaying = Index;
                if (YoutubeVideo.Movie == null)
                {
                    YoutubeVideo.Movie = PlaylistURLs[Index];
                }
                Internal_Command = true;
                YoutubeVideo_CallFlash("loadVideoByUrl(" + PlaylistURLs[Index] + ")");
                YoutubeVideo_CallFlash("playVideo()");
                Internal_Command = false;

                Playlist.Refresh();

                if (!RecursiveFlag) BroadcastCurrentlyPlaying();
            }
        }

        // Called in order to remove a video from the playlist given an index
        private void DeleteVideo(int Index)
        {
            string Title = Playlist.Items[Index].ToString();
            PlaylistURLs.RemoveAt(Index);
            Playlist.Items.RemoveAt(Index);
            UpdatePlaylistCount();

            if (Hosting)
            {
                Broadcast("has removed " + Title + " from the playlist", NicknameLabel.Text, false);
                BroadcastPlaylist();
            }
            else BroadcastPlaylist("has removed " + Title + " from the playlist");
            Thread.Sleep(100);

            if (Index == CurrentlyPlaying)
            {
                if (PlaylistURLs.Count <= Index && PlaylistURLs.Count != 0)
                {
                    PlayVideo(PlaylistURLs.Count - 1, false);
                }
                else if (PlaylistURLs.Count == 0)
                {
                    YoutubeVideo.Movie = null;
                }
                else PlayVideo(CurrentlyPlaying, false);
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

        // Listener function. Host listens for incoming TCP connections and begins speak threads.
        public void Listen(object sender, DoWorkEventArgs e)
        {
            Chat("Initiating chat service...", "CONSOLE");

            ServerSocket = new TcpListener(IPAddress.Any, HostPort);
            ClientSocket = default(TcpClient);
            bool Started = true;

            ClientsList = new Hashtable();

            try { ServerSocket.Start(); }
            catch
            {
                Chat("Unable to bind to port.", "CONSOLE");
                Started = false;
            }

            Hosting = true;

            while (Started)
            {
                // Listen
                try { ClientSocket = ServerSocket.AcceptTcpClient(); }
                catch { return; }


                //Open ProgramSettings for IPList Info
                string PathName = @"ProgramSettings.conf";
                int ListType;
                String line;
                bool allowed = true;
                string[] connector = ClientSocket.Client.RemoteEndPoint.ToString().Split(':');
                    if (System.IO.File.Exists(PathName))
                    {
                        try
                        {
                            using (StreamReader readFile = new StreamReader(PathName))
                            {
                                //read in user data
                                ListType = Convert.ToInt32(line = readFile.ReadLine());
                                line = readFile.ReadLine();
                                allowed = Convert.ToBoolean(1 - ListType);

                                //read in connection information from file
                                while ((line = readFile.ReadLine()) != null)
                                {
                                    line = readFile.ReadLine();

                                    if (connector[0] == line)
                                    {
                                        allowed = !allowed;
                                        break;
                                    }
                                }
                                readFile.Close();
                            }

                        }
                        catch{}
                    }
                    else
                    {
                        using (System.IO.FileStream fs = System.IO.File.Create(PathName))
                        {
                            for (byte i = 0; i < 100; i++)
                            {
                                fs.WriteByte(i);
                            }
                        }
                    }
                //if the person is not allowed in, skip.
                if( allowed)
                {
                    byte[] BytesIn = new byte[65536];
                    string DataFromClient;

                    // Get name of new client
                    NetworkStream Stream = ClientSocket.GetStream();
                    Stream.Read(BytesIn, 0, (int)ClientSocket.ReceiveBufferSize);
                    DataFromClient = Decrypt(System.Text.Encoding.UTF8.GetString(BytesIn));

                    // If somehow they have the same name as someone else
                    bool Connected = true;
                    try { ClientsList.Add(DataFromClient, ClientSocket); }
                    catch
                    {
                        NetworkStream OutStream = ClientSocket.GetStream();
                        Byte[] BroadcastBytes = Encoding.UTF8.GetBytes(Encrypt("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] CONSOLE: Server declined connection: USERNAME COLLISION") + "$");
                        OutStream.Write(BroadcastBytes, 0, BroadcastBytes.Length);
                        OutStream.Flush();
                        Connected = false;
                    }

                    if (Connected)
                    {
                        // Display appropriate messages
                        Broadcast("has joined the room", DataFromClient, false);

                        // Launch speaker thread
                        Thread ChatThread2 = new Thread(() => Speak(ClientSocket, DataFromClient));
                        ChatClient2Threads.Add(ChatThread2);
                        ChatThread2.Start();

                        // Send list of clients & playlist to everyone
                        Thread.Sleep(200);
                        BroadcastClientsList();
                        Thread.Sleep(200);
                        BroadcastPlaylist();
                        Thread.Sleep(200);
                        BroadcastCurrentlyPlaying();
                        Thread.Sleep(200);
                        Broadcast("PLAY", "", false);
                    }
                    else Connected = true;
                }
            }

            Hosting = false;

            try
            {
                ClientSocket.Close();
                ServerSocket.Stop();
            }
            catch { }
            Chat("Terminating chat service...", "CONSOLE");
        }

        // Host only function. Sends message over TCP to all connected entities.
        public static void Broadcast(string Message, string Entity, bool ClientFlag)
        {
            if (Message == "") return;
            try
            {
                foreach (DictionaryEntry Item in ClientsList)
                {
                    TcpClient BroadcastSocket = (TcpClient)Item.Value;

                    NetworkStream BroadcastStream = BroadcastSocket.GetStream();
                    Byte[] BroadcastBytes;

                    // Empty entity is information transfer not to be displayed in chat
                    if (Entity == "")
                    {
                        string[] Command = Message.Split(' ');
                        switch (Command[0])
                        {
                            case "PLAY":
                            case "PAUSE":
                            case "CURRENTLYPLAYING":
                            case "NEWCLIENTSLIST":
                            case "NEWPLAYLIST":
                            case "VOTETOSKIP":
                            case "VOTE":
                                BroadcastBytes = Encoding.UTF8.GetBytes(Encrypt(Message) + "$");
                                break;
                            default:
                                BroadcastBytes = Encoding.UTF8.GetBytes(Encrypt("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] CONSOLE: An unexpected error occured.") + "$");
                                break;
                        }
                    }
                    else
                    {
                        // When info sent is a message to be displayed client flag indicates ownership of message
                        if (ClientFlag)
                        {
                            BroadcastBytes = Encoding.UTF8.GetBytes(Encrypt("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message) + "$");
                        }
                        else
                        {
                            BroadcastBytes = Encoding.UTF8.GetBytes(Encrypt("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + " " + Message) + "$");
                        }
                    }

                    BroadcastStream.Write(BroadcastBytes, 0, BroadcastBytes.Length);
                    BroadcastStream.Flush();
                }
            }
            catch { }
        }

        // Used when clients join/leave. Allows chatting members to recieve list of who they speak to
        private void BroadcastClientsList()
        {
            string Clients = "NEWCLIENTSLIST ";
            foreach (DictionaryEntry Name in ClientsList)
            {
                Clients += Name.Key;
                Clients += " ";
            }

            Broadcast(Clients, "", false);
        }

        // Host function. Keeps playlist up to date in all clients.
        private void BroadcastPlaylist()
        {
            string Playlist = "NEWPLAYLIST ";
            foreach (string url in PlaylistURLs)
            {
                Playlist += url;
                Playlist += " ";
            }

            Broadcast(Playlist, "", false);
        }

        // Client Function. Sends new playlist info to host
        private void BroadcastPlaylist(string Message)
        {
                string Playlist = "NEWPLAYLIST ";
                foreach (string url in PlaylistURLs)
                {
                    Playlist += url;
                    Playlist += " ";
                }

                ClientBroadcast("PLAYLIST$" + Encrypt(Playlist) + "$" + Encrypt(Message) + "$");
        }

        // Host and Client Function. Sends currently playing info to host/other clients.
        private void BroadcastCurrentlyPlaying()
        {
            string Playing = "CURRENTLYPLAYING ";
            Playing += CurrentlyPlaying.ToString();

            if (CurrentlyPlaying >= 0)
            {
                string sCurrentTime = YoutubeVideo_CallFlash("getCurrentTime()");
                if (sCurrentTime != "")
                {
                    sCurrentTime = sCurrentTime.Remove(sCurrentTime.Length - 9).Remove(0, 8);
                    Playing += " " + sCurrentTime;
                }
            }

            if (Hosting) Broadcast(Playing, "", false);
            else
            {
                ClientBroadcast("CURRENTLYPLAYING$" + Encrypt(CurrentlyPlaying.ToString()) + "$");
            }
        }

        // One function thread for each TCP connection host keeps track of. Used to recieve and process messages from clients in the host.
        private void Speak(TcpClient ClientSocket, string Entity)
        {
            byte[] BytesFrom = new byte[65536];

            while (true)
            {
                try
                {
                    NetworkStream Stream = ClientSocket.GetStream();
                    Stream.Read(BytesFrom, 0, (int)ClientSocket.ReceiveBufferSize);
                    // Splits message on $ 
                    string[] Message = System.Text.Encoding.UTF8.GetString(BytesFrom).Split('$');
                    if (Message[0] == "")
                    {
                        ClientsList.Remove(Entity);
                        Broadcast("has left the room", Entity, false);
                        BroadcastClientsList();
                        return;
                    }
                    // Client communication handling
                    else
                    {
                        switch (Message[0])
                        {
                            case "PLAYLIST":
                                Message[1] = Decrypt(Message[1] + "$");
                                Message[2] = Decrypt(Message[2] + "$");
                                PlaylistUpdate(Message[1].Split(' '));
                                if (Message[2] != "")
                                {
                                    Broadcast(Message[2], Entity, false);
                                }
                                Thread.Sleep(200);
                                BroadcastPlaylist();
                                break;
                            case "CURRENTLYPLAYING":
                                Message[1] = Decrypt(Message[1] + "$");
                                try { PlayVideo(Convert.ToInt32(Message[1]), false); }
                                catch { }
                                break;
                            case "PAUSE":
                                Broadcast("PAUSE", "", false);
                                Internal_Command = true;
                                YoutubeVideo_CallFlash("pauseVideo()");
                                Internal_Command = false;
                                break;
                            case "PLAY":
                                Broadcast("PLAY " + Message[1], "", false);
                                Internal_Command = true;
                                YoutubeVideo_CallFlash("playVideo()");
                                Internal_Command = false;
                                break;
                            case "REFRESH":
                                string sCurrentTime = YoutubeVideo_CallFlash("getCurrentTime()");
                                sCurrentTime = sCurrentTime.Remove(sCurrentTime.Length - 9).Remove(0, 8);
                                Broadcast("PLAY " + sCurrentTime, "", false);
                                Internal_Command = true;
                                YoutubeVideo_CallFlash("playVideo()");
                                Internal_Command = false;
                                break;
                            case "VOTETOSKIP":
                                Broadcast("VOTETOSKIP", "", false);
                                CallTimer();
                                break;
                            case "VOTE":
                                VoteCounter++;
                                break;
                            default:
                                Message[1] = Decrypt(Message[1] + "$");
                                Broadcast(Message[1], Entity, true);
                                break;
                        }
                    }
                }
                catch
                {
                    ClientsList.Remove(Entity);
                    Broadcast("has left the room", Entity, false);
                    BroadcastClientsList();
                    return;
                }
                Thread.Sleep(200);
            }
        }

        // Client function. Activates whenever clients are connected. Gets messages from host
        public void GetMessage(object sender, DoWorkEventArgs e)
        {
            while (SelfSocket != null && SelfSocket.Connected)
            {
                SelfStream = SelfSocket.GetStream();
                byte[] InStream = new byte[65536];
                try 
                {
                    if (SelfSocket.Connected)
                    {
                        SelfStream.Read(InStream, 0, SelfSocket.ReceiveBufferSize);
                    }
                }
                catch { break; }
                string Mess = Decrypt(Encoding.UTF8.GetString(InStream));
                string[] Message = Mess.Split(' ');

                // How to deal with message, first word delim by spaces determines message use
                switch (Message[0])
                {
                    // Incoming new list of chatting members
                    case "NEWCLIENTSLIST":
                        ClientListUpdate(Message);
                        break;
                    // Incoming new playlist information
                    case "NEWPLAYLIST":
                        PlaylistUpdate(Message);
                        break;
                    // Incoming Currently Playing
                    case "CURRENTLYPLAYING":
                        int Play = Convert.ToInt32(Message[1]);
                        if (Play >= 0 && !Hosting && Play < Playlist.Items.Count)
                        {
                            Internal_Command = true;
                            PlayVideo(Play, true);
                            if (Message.Length == 3)
                            {
                                ClientBroadcast("REFRESH$");
                            }
                            Internal_Command = false;
                        }
                        break;
                    // Video pause flag sent
                    case "PAUSE":
                        if (YoutubeVideo_CallFlash("getPlayerState()") == "<number>1</number>" && !Seek_Immunity)
                        {
                            Internal_Command = true;
                            YoutubeVideo_CallFlash("pauseVideo()");
                            Internal_Command = false;
                        }
                        break;
                    case "PLAY":
                        if (YoutubeVideo_CallFlash("getCurrentTime()") == "" || Message.Length == 1) break;
                        Seek_Immunity = true;
                        Internal_Command = true;
                        string sCurrentTime = YoutubeVideo_CallFlash("getCurrentTime()");
                        sCurrentTime = sCurrentTime.Remove(sCurrentTime.Length - 9).Remove(0, 8);
                        double CurrentTime = Convert.ToDouble(sCurrentTime);
                        double SynchTime = Convert.ToDouble(Message[1]);
                        if (Math.Abs(CurrentTime - SynchTime) >= 0.5f)
                        {
                            SynchTime += 0.3f;
                            YoutubeVideo_CallFlash("seekTo(" + SynchTime.ToString() + ", true)");
                        }
                        YoutubeVideo_CallFlash("playVideo()");
                        Seek_Immunity = false;
                        Internal_Command = false;
                        break;
                    case "VOTETOSKIP":
                        CallTimer();
                        break;
                    // The usual chat message
                    default:
                        string TotalMessage = "";
                        foreach (string s in Message)
                        {
                            TotalMessage += s;
                            TotalMessage += " ";
                        }
                        TotalMessage = TotalMessage.Substring(0, TotalMessage.Length - 1);
                        ClientChat(TotalMessage);
                        break;
                }
                Thread.Sleep(200);
            }
            Chat("Lost connection from server...", "CONSOLE");

            Connected = false;
        }

        // Helpful function for client speach. Checks for connection before writing.
        private void ClientBroadcast(string Message)
        {
            try
            {
                byte[] Chat = Encoding.UTF8.GetBytes(Message);
                SelfStream.Write(Chat, 0, Chat.Length);
                SelfStream.Flush();
            }
            catch { }
        }

        // Called from inside non-main thread thus invoke required. Updates the chatting members when new list info is had.
        private void ClientListUpdate(string[] Message)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action<string[]>(ClientListUpdate), new object[] { Message }); }
                catch { return; }
            }
            else
            {
                ChatMembers.Items.Clear();
                for (int i = 1; i < Message.Length - 1; i++)
                {
                    ChatMembers.Items.Add(Message[i]);
                }
                ChatLabel.Text = "Chatting: " + ChatMembers.Items.Count;
            }
        }

        // Called from inside non-main thread thus invoke required. Updates the playlist when new list info is had.
        private void PlaylistUpdate(string[] Message)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action<string[]>(PlaylistUpdate), new object[] { Message }); }
                catch { return; }
            }
            else
            {
                string CurrentVideoName = "Nothing";
                int CurrentVideoIndex = 0;
                int NewVideoIndex = 0;
                if (PlaylistURLs.Count > 0 && CurrentlyPlaying > -1)
                {
                   CurrentVideoName = PlaylistURLs[CurrentlyPlaying];
                   CurrentVideoIndex = CurrentlyPlaying;
                   NewVideoIndex = 0;
                }
                List<string> NewPlaylistURLs = new List<string>();
                List<string> NewPlaylistTitles = new List<string>();

                for (int i = 1; i < Message.Length - 1; i++)
                {
                    NewPlaylistURLs.Add(Message[i]);
                    if (!PlaylistURLs.Contains(Message[i]))
                    {
                        Video video = RequestFromYoutube(ConvertURLToBrowser(Message[i]));
                        NewPlaylistTitles.Add(video.Snippet.Title);
                    }
                    else NewPlaylistTitles.Add(Playlist.Items[PlaylistURLs.IndexOf(Message[i])].ToString());
                }
                PlaylistURLs = NewPlaylistURLs;

                Playlist.Items.Clear();
                foreach (string s in NewPlaylistTitles)
                {
                    Playlist.Items.Add(s);
                }

                PlaylistLabel.Text = "Playlist Count: " + Playlist.Items.Count;

                if (PlaylistURLs.Count > 0 && CurrentlyPlaying > -1)
                {
                    for (int i = 0; i < PlaylistURLs.Count; i++)
                    {
                        if (PlaylistURLs[i] == CurrentVideoName)
                        {
                            NewVideoIndex = i;
                        }
                    }

                    
                 CurrentlyPlaying = NewVideoIndex;
                 Playlist.Refresh();
                    
                }

            }
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
            if (Message != "")
            {
                if (ChatHistory.Text == "")
                    ChatHistory.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message);
                else ChatHistory.AppendText("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message);
            }
        }

        // Mostly deprecated. Use only for SELF sent messages. Seen by only the client this function is called in.
        private void ClientChat(string Msg)
        {
            if (Msg != "")
            {
                if (this.InvokeRequired)
                    try { this.Invoke(new Action<string>(ClientChat), new object[] { Msg }); }
                    catch { return; }
                else
                    ChatHistory.AppendText(Msg);
            }
        }

        // Calls the timeX_Tick function
        void CallTimer()
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke(new Action(CallTimer), new object[] { }); }
                catch { return; }
            }
            else if(CurrentlyPlaying > -1)
            {
                timer.Tick += new EventHandler(timeX_Tick);
                timer.Enabled = true;
            }
            else
            {
                MessageBox.Show("No video is playing");
                beginVoteMenuItem.Enabled = true;
            }
        }

        // Vote Time count down
        void timeX_Tick(object sender, EventArgs e)
        {
            TimeSpan t = TimeSpan.FromSeconds(VoteTime);
            if (VoteTime > 0 && CurrentlyPlaying > -1)
            {
                if (VoteTime % 5 == 0)
                {
                    Chat(string.Format("{0:D2}", t.Seconds) + " seconds left to vote", "CONSOLE");
                }
                VoteTime--;

                if (NumOfVotes > 0)
                {
                    NumOfVotes = 0;
                    VoteResult = MessageBox.Show("Would you like to skip this video?", "Popcorn Viewer Vote", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (VoteResult == DialogResult.Yes)
                    {
                        if (Hosting) VoteCounter++;
                        else
                        {
                            ClientBroadcast("VOTE");
                        }
                    }
                    VoteResult = DialogResult.Ignore;
                }
            }
            else if(CurrentlyPlaying > -1)
            {
                Chat(string.Format("{0:D2}", t.Seconds) + " seconds left to vote", "CONSOLE");
                NumOfVotes = 1;
                timer.Enabled = false;
                timer = new System.Windows.Forms.Timer() { Interval = 1000 };
                VoteTime = SECONDS_TO_VOTE;
                beginVoteMenuItem.Enabled = true;
                if (Hosting)
                {
                    Broadcast(VoteCounter + "/" + ChatMembers.Items.Count + " members voted to skip the video.", "CONSOLE", true);
                    if (VoteCounter > (float)ChatMembers.Items.Count / 2)
                    {
                        Broadcast("has voted to skip " + Playlist.Items[CurrentlyPlaying].ToString(), "The party", false);
                        Thread.Sleep(200);
                        if (PlaylistURLs.Count - 1 > CurrentlyPlaying)
                        {
                            PlayVideo(CurrentlyPlaying + 1, false);
                        }
                        else PlayVideo(0, false);
                    }
                    else Broadcast("fails to skip " + Playlist.Items[CurrentlyPlaying].ToString(), "The party", false);
                    VoteCounter = 0;
                }
            }
        }
    }
}

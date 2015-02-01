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

                byte[] BytesIn = new byte[65536];
                string DataFromClient;

                // Get name of new client
                NetworkStream Stream = ClientSocket.GetStream();
                Stream.Read(BytesIn, 0, (int)ClientSocket.ReceiveBufferSize);
                DataFromClient = System.Text.Encoding.UTF8.GetString(BytesIn);
                DataFromClient = DataFromClient.Substring(0, DataFromClient.IndexOf("$"));

                // If somehow they have the same name as someone else
                try { ClientsList.Add(DataFromClient, ClientSocket); }
                catch
                {
                    Broadcast("Client hash colission! Host terminating session...", "CONSOLE", true);
                    return;
                }

                // Display appropriate messages
                Broadcast("has joined the room", DataFromClient, false);

                // Launch speaker thread
                Thread ChatThread2 = new Thread(() => Speak(ClientSocket, DataFromClient));
                ChatClient2Threads.Add(ChatThread2);
                ChatThread2.Start();

                // Send list of clients to everyone
                BroadcastClientsList();
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

        public static void Broadcast(string Message, string Entity, bool ClientFlag)
        {
            foreach (DictionaryEntry Item in ClientsList)
            {
                TcpClient BroadcastSocket = (TcpClient)Item.Value;
                NetworkStream BroadcastStream = BroadcastSocket.GetStream();
                Byte[] BroadcastBytes;

                // Empty entity is information transfer
                if (Entity == "")
                {
                    string[] Command = Message.Split(' ');
                    switch (Command[0])
                    {
                        case "NEWCLIENTSLIST":
                            BroadcastBytes = Encoding.UTF8.GetBytes(Message);
                            break;
                        default:
                            BroadcastBytes = Encoding.UTF8.GetBytes("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] CONSOLE: An unexpected error occured.");
                            break;
                    }
                }
                else
                {
                    if (ClientFlag)
                    {
                        BroadcastBytes = Encoding.UTF8.GetBytes("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + ": " + Message);
                    }
                    else
                    {
                        BroadcastBytes = Encoding.UTF8.GetBytes("\n[" + DateTime.Now.ToString("HH:mm:ss") + "] " + Entity + " " + Message);
                    }
                }

                BroadcastStream.Write(BroadcastBytes, 0, BroadcastBytes.Length);
                BroadcastStream.Flush();
            }
        }

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
                    DataFromClient = System.Text.Encoding.UTF8.GetString(BytesFrom);
                    DataFromClient = DataFromClient.Substring(0, DataFromClient.IndexOf("$"));
                    Broadcast(DataFromClient, Entity, true);
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

        public void GetMessage(object sender, DoWorkEventArgs e)
        {
            while (SelfSocket != null && SelfSocket.Connected)
            {
                SelfStream = SelfSocket.GetStream();
                byte[] InStream = new byte[65536];
                try { SelfStream.Read(InStream, 0, SelfSocket.ReceiveBufferSize); }
                catch { break; }
                string[] Message = Encoding.UTF8.GetString(InStream).Split(' ');

                switch (Message[0])
                {
                    case "NEWCLIENTSLIST":
                        ClientListUpdate(Message);
                        break;
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
        }

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
                    try { this.Invoke(new Action<string>(ClientChat), new object[] { Msg }); }
                    catch { return; }
                else
                    ChatHistory.AppendText(Msg);
            }
        }
    }
}

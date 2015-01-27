using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using Google.YouTube;

namespace PopcornViewer
{
    public partial class MainForm : Form
    {
        #region Local Variables
        
        // Form Variables
        List<string> PlaylistURLs = new List<string>();
        // Index in PlaylistURLs of currently playing video
        int CurrentlyPlaying = -1;

        // Playlist Drag/Drop Variables
        bool PlaylistDragging = false;
        int SavedX = 0;
        int SavedY = 0;
        int LastSelectedItem = -1;
        const int TOLERANCE = 2;

        #endregion

        #region Utility Functions

        /// <summary>
        /// URL Conversion Tools
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Adds videos to Playlist given a browser URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Changes the text on the PlaylistLabel to reflect number of videos added
        /// </summary>
        private void UpdatePlaylistCount()
        {
            PlaylistLabel.Text = PlaylistLabel.Text.Substring(0, PlaylistLabel.Text.Length - 1);
            PlaylistLabel.Text = PlaylistLabel.Text + Playlist.Items.Count;
        }

        /// <summary>
        /// Ensures that the given URL is actually for a Youtube video.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Code for handling Youtube requests from a specific URL. Requires a Youtube link be verrified first.
        /// </summary>
        /// <param name="url"></param>
        private Video RequestFromYoutube(string url)
        {
            string uri = url.Remove(0, url.IndexOf('=') + 1);

            YouTubeRequestSettings settings = new YouTubeRequestSettings("Popcorn Viewer", "AI39si4LgRzD-nVk4ZIHLC5pLti7cBcVLKKhJIS7PCyosewQMlAVgSqtCKMfzTTLwScr4qV6UxeDFo7YsfjBaEdLn3lVJocjbA");
            YouTubeRequest req = new YouTubeRequest(settings);

            Uri videoEntryUrl = new Uri("http://gdata.youtube.com/feeds/api/videos/" + uri);

            return req.Retrieve<Video>(videoEntryUrl);
        }

        /// <summary>
        /// Performs bundled functions to play a video at URL index
        /// </summary>
        /// <param name="Index"></param>
        private void PlayVideo(int Index)
        {
            CurrentlyPlaying = Index;
            if (YoutubeVideo.Movie == null)
            {
                YoutubeVideo.Movie = PlaylistURLs[Index];
            }
            YoutubeVideo_CallFlash("loadVideoByUrl(" + PlaylistURLs[Index] + ")");
            YoutubeVideo_CallFlash("playVideo()");
        }

        #endregion

        #region Flash-C# Communication

        /// <summary>
        /// Handles the Flash -> C# communication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Calls functions in the Flash Player
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates flash args
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="delim"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Main function
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
            Playlist.DrawMode = DrawMode.OwnerDrawFixed;
            Playlist.DrawItem += new DrawItemEventHandler(Playlist_DrawItem);
        }

        /// <summary>
        /// Functions controling Playlist mouse and keyboard operations
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Delete)
            {
                if (Playlist.SelectedIndex >= 0)
                {
                    PlaylistURLs.RemoveAt(Playlist.SelectedIndex);
                    Playlist.Items.RemoveAt(Playlist.SelectedIndex);
                    UpdatePlaylistCount();
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
                
                if (LastSelectedItem == -1)
                {
                    PlayVideo(Playlist.SelectedIndex);
                    LastSelectedItem = CurrentlyPlaying;
                    Playlist.Items.Add("FixRed");
                    Playlist.SelectedIndex = 1;
                    Playlist.SelectedIndex = CurrentlyPlaying;
                    Playlist.Items.Remove("FixRed");
                }
                else
                {
                    PlayVideo(Playlist.SelectedIndex);
                    Playlist.SelectedIndex = LastSelectedItem;
                    Playlist.SelectedIndex = CurrentlyPlaying;
                    LastSelectedItem = CurrentlyPlaying;
                }
                
            }
        }

        /// <summary>
        /// Enables and disables appropriate options on opening
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// These functions define behaviors for Playlist context menu items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyPlaylistMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ConvertURLToBrowser(PlaylistURLs[Playlist.SelectedIndex]));
        }

        private void deletePlaylistMenuItem_Click(object sender, EventArgs e)
        {
            if (Playlist.SelectedIndex < CurrentlyPlaying)
            {
                CurrentlyPlaying--;
                Playlist.Items.Add("FixRed");
                Playlist.SelectedIndex = 1;
                Playlist.SelectedIndex = CurrentlyPlaying;
                Playlist.Items.Remove("FixRed");
            }
            if (Playlist.SelectedIndex == CurrentlyPlaying)
            {
                CurrentlyPlaying = -1;

            }
            PlaylistURLs.RemoveAt(Playlist.SelectedIndex);
            LastSelectedItem = -1;
            Playlist.Items.RemoveAt(Playlist.SelectedIndex);
            UpdatePlaylistCount();
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

                if (LastSelectedItem == -1)
                {
                    PlayVideo(Playlist.SelectedIndex);
                    LastSelectedItem = CurrentlyPlaying;
                    Playlist.Items.Add("FixRed");
                    Playlist.SelectedIndex = 1;
                    Playlist.SelectedIndex = CurrentlyPlaying;
                    Playlist.Items.Remove("FixRed");
                }
                else
                {
                    PlayVideo(Playlist.SelectedIndex);
                    Playlist.SelectedIndex = LastSelectedItem;
                    Playlist.SelectedIndex = CurrentlyPlaying;
                    LastSelectedItem = CurrentlyPlaying;
                }

            }
        }

        /// <summary>
        /// Handles logic for drag and drop in the Playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                Playlist.Items.Remove(data);
                Playlist.Items.Insert(index, data);
                PlaylistURLs.Remove(ind);
                PlaylistURLs.Insert(index, ind);
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

        /// <summary>
        /// Controls the edit Toolstrip Menu items' logic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        //Changes the Playlist item 0 to be red showing that it is the one currently playing
        private void Playlist_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
        {

                // Draw the background of the ListBox control for each item.
                e.DrawBackground();
                // Define the default color of the brush as black.
                Brush myBrush = Brushes.Black;

                // Determine the color of the brush to draw each item based  
                // on the index of the item to draw. 
                if (e.Index == CurrentlyPlaying)
                {
                    myBrush = Brushes.Red;
                }

                // Draw the current item text based on the current Font  
                // and the custom brush settings.
            if(e.Index > -1)
            { 
                e.Graphics.DrawString(Playlist.Items[e.Index].ToString(),
                    e.Font, myBrush, e.Bounds, StringFormat.GenericDefault);
            }
                // If the ListBox has focus, draw a focus rectangle around the selected item.
                e.DrawFocusRectangle();
            
        }

        #endregion
    }
}

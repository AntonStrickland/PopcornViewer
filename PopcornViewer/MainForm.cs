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
        // Form Variables
        List<string> PlaylistURLs = new List<string>();
        int CurrentlyPlaying;

        /// <summary>
        /// URL Conversion Tools
        /// </summary>
        /// <param name="URL"></param>
        /// <returns></returns>
        // Browser to Embedded
        private string ConvertURLToEmbeded(string url)
        {
            url = url.Replace("/watch?", "/");
            url = url.Replace('=', '/');
            url = url + "?version=3&enablejsapi=1";
            return url;
        }

        // Embedded to Browser
        private string ConvertURLToBrowser(string url)
        {
            url = url.Replace("?version=3&enablejsapi=1", "");
            url = url.Replace("/v/", "/watch?v=");
            return url;
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
                    return ((response.ResponseUri.ToString().Contains("youtube.com") &&
                            response.ResponseUri.ToString().Contains("watch?v=")));
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

        //
        // FORM EVENT FUNCTIONS
        //

        /// <summary>
        /// Main function
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles logic for drag and drop in the Playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_DragDrop(object sender, DragEventArgs e)
        {
            string url = (string) e.Data.GetData(DataFormats.Text, false);
            if (IsYoutubeURL(url))
            {
                PlaylistURLs.Add(ConvertURLToEmbeded(url));
                Video video = RequestFromYoutube(url);
                Playlist.Items.Add(video.Title);
                UpdatePlaylistCount();
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
                    YoutubeVideo.Movie = PlaylistURLs[Playlist.SelectedIndex];
                    CurrentlyPlaying = Playlist.SelectedIndex;
                }
            }
        }
    }
}

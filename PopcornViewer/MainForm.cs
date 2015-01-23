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

using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.YouTube;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;

namespace PopcornViewer
{
    public partial class MainForm : Form
    {
        // Form Variables
        List<string> PlayListURLs = new List<string>();

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
                PlayListURLs.Add(ConvertURLToEmbeded(url));
                Video video = RequestFromYoutube(url);
                Playlist.Items.Add(video.Title);
                PlaylistLabel.Text = PlaylistLabel.Text.Substring(0, PlaylistLabel.Text.Length - 1);
                PlaylistLabel.Text = PlaylistLabel.Text + PlayListURLs.Count;
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
        /// Ensures that the given URL is actuall for a Youtube video.
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
    }
}

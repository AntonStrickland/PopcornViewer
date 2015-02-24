using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Collections;
using System.Text.RegularExpressions;
using Google.YouTube;

namespace PopcornViewer
{
    public partial class MainForm
    {
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
                            YoutubeVideo_CallFlash("playVideo()");
                            break;

                        // Ended
                        case 0:
                            // To avoid having clients finish shortly before/after
                            if (Hosting)
                            {
                                // Repeat All
                                if (repeatAllToolStripMenuItem.Checked)
                                {
                                    if (PlaylistURLs.Count - 1 > CurrentlyPlaying)
                                    {
                                        PlayVideo(CurrentlyPlaying + 1, false);
                                    }
                                    else PlayVideo(0, false);
                                }

                                // Play Next
                                else if (playNextToolStripMenuItem.Checked)
                                {
                                    if (PlaylistURLs.Count - 1 > CurrentlyPlaying)
                                    {
                                        PlayVideo(CurrentlyPlaying + 1, false);
                                    }
                                }

                                // Repeat One
                                else if (repeatOneToolStripMenuItem.Checked)
                                {
                                    PlayVideo(CurrentlyPlaying, false);
                                }

                                // Shuffle
                                else if (shuffleToolStripMenuItem.Checked)
                                {
                                    Random random = new Random();
                                    int nextVideo = random.Next(0, PlaylistURLs.Count);

                                    PlayVideo(nextVideo, false);
                                }
                            }
                            break;

                        // Playing
                        case 1:
                            if (Hosting) Broadcast("PLAY", "", false);
                            else
                            {
                                byte[] Chat = Encoding.UTF8.GetBytes("PLAY$");
                                SelfStream.Write(Chat, 0, Chat.Length);
                                SelfStream.Flush();
                            }
                            break;

                        // Paused
                        case 2:
                            if (YoutubeVideo_CallFlash("getCurrentTime()") != YoutubeVideo_CallFlash("getDuration()"))
                            { 
                                if (Hosting) Broadcast("PAUSE", "", false);
                                else
                                {
                                    byte[] Chat = Encoding.UTF8.GetBytes("PAUSE$");
                                    SelfStream.Write(Chat, 0, Chat.Length);
                                    SelfStream.Flush();
                                }
                            }
                            break;

                        // Buffering
                        case 3:
                            if (YoutubeVideo_CallFlash("getCurrentTime()") != YoutubeVideo_CallFlash("getDuration()"))
                            {
                                if (Hosting) Broadcast("PAUSE", "", false);
                                else
                                {
                                    byte[] Chat = Encoding.UTF8.GetBytes("PAUSE$");
                                    SelfStream.Write(Chat, 0, Chat.Length);
                                    SelfStream.Flush();
                                }

                                string sBuffered = YoutubeVideo_CallFlash("getVideoLoadedFraction()");
                                sBuffered = sBuffered.Remove(0, 7);
                                sBuffered = sBuffered.Remove(sBuffered.Length - 8);
                                Double Buffered = Convert.ToDouble(sBuffered);
                                Double TargetBuffered = Buffered + 0.1f;
                                if (TargetBuffered > 1.0f) TargetBuffered = 1.0f;

                                string sTarget = YoutubeVideo_CallFlash("getCurrentTime()");
                                sTarget = sTarget.Remove(0, 8);
                                sTarget = sTarget.Remove(sTarget.Length - 9);
                                Double Target = Convert.ToDouble(sTarget) + 10;

                                string sDuration = YoutubeVideo_CallFlash("getDuration()");
                                sDuration = sDuration.Remove(0, 8);
                                sDuration = sDuration.Remove(sDuration.Length - 9);
                                Double Duration = Convert.ToDouble(sDuration);
                                
                                // Whichever comes first, 10% of video in front processed or 10 seconds or whole vid
                                while (Buffered < (Target / Duration) && Buffered < TargetBuffered)
                                {
                                    Thread.Sleep(100);
                                    sBuffered = YoutubeVideo_CallFlash("getVideoLoadedFraction()");
                                    sBuffered = sBuffered.Remove(0, 7);
                                    sBuffered = sBuffered.Remove(sBuffered.Length - 8);
                                    Buffered = Convert.ToDouble(sBuffered);
                                }

                                if (Hosting) Broadcast("PLAY", "", false);
                                else
                                {
                                    byte[] Chat = Encoding.UTF8.GetBytes("PLAY$");
                                    SelfStream.Write(Chat, 0, Chat.Length);
                                    SelfStream.Flush();
                                }
                            }
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
    }
}

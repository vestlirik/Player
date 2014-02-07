using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace vkAudio
{
    public class AudioVK:Audio
    {
        private System.Xml.XmlNode xmlNode;

        public AudioVK(System.Xml.XmlNode xmlNode)
        {
            this.xmlNode = xmlNode;

            this.aid = xmlNode["aid"].InnerText;
            this.owner_id = xmlNode["owner_id"].InnerText;
            this.artist = xmlNode["artist"].InnerText;
            this.title = xmlNode["title"].InnerText;
            base.URL = xmlNode["url"].InnerText;
            this.duration = xmlNode["duration"].InnerText;
            try
            {
                this.lyrics_id = xmlNode["lyrics_id"].InnerText;
            }
            catch
            {
                this.lyrics_id = "";
            }
            try
            {
                this.genre_id = xmlNode["genre_id"].InnerText;
            }
            catch
            {
                this.genre_id = "";
            }

            this.Name = artist + " - " + title;

        }

        //get lyrics of track
        public string GetLirycs(string token)
        {
            if (lyrics_id != "")
            {
                try
                {
                    Uri uri = new Uri("https://api.vk.com/method/audio.getLyrics.xml?lyrics_id=" + lyrics_id + "&access_token=" + token);
                    var x = new XmlDocument();
                    x.Load(uri.ToString());
                    var el = x.GetElementsByTagName("response")[0];
                    return el.ChildNodes[0].LastChild.InnerText;
                }
                catch
                {
                    return "";
                }
            }
            return "";
        }

        public string aid { get; protected set; }
        public string owner_id { get; protected set; }
        public string artist { get; protected set; }
        public string title { get; protected set; }
        public string duration { get; protected set; }

        public string DurationString
        {
            get
            {
                TimeSpan tmp = TimeSpan.FromSeconds(double.Parse(duration));
                return tmp.Minutes + ":" + String.Format("{0:00}", tmp.Seconds);
            }
        }
        public string lyrics_id { get; protected set; }
        public string genre_id { get; protected set; }
    }
}

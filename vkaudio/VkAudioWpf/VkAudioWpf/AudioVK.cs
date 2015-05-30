using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace VkAudioWpf
{
    public class AudioVK : Audio, IComparable<AudioVK>
    {
        private System.Xml.XmlNode xmlNode;
        public string aid { get; protected set; }
        public string owner_id { get; protected set; }
        public string artist { get; set; }
        public string title { get; set; }
        public string duration { get; protected set; }
        public string lyrics_id { get; protected set; }
        public string album_id { get; protected set; }
        public string genre_id { get; protected set; }
        public bool IsAdded { get; set; }

        public AudioVK(System.Xml.XmlNode xmlNode)
        {
            this.xmlNode = xmlNode;

            this.aid = xmlNode["id"].InnerText;
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
                this.album_id = xmlNode["album_id"].InnerText;
            }
            catch
            {
                this.album_id = "";
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

            IsAdded = false;

        }

        //get lyrics of track
        public string GetLyrics(string token)
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

        //add to my audio
        public bool Add(string token)
        {
            try
            {
                Uri uri = new Uri("https://api.vk.com/method/audio.add.xml?audio_id=" + aid + "&owner_id=" + owner_id + "&access_token=" + token);
                var x = new XmlDocument();
                x.Load(uri.ToString());
                var el = x.GetElementsByTagName("response")[0].InnerText;
                int z;
                bool b = Int32.TryParse(el,out z);
                if (b)
                {
                    IsAdded = true;
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }


        public string DurationString
        {
            get
            {
                TimeSpan tmp = TimeSpan.FromSeconds(double.Parse(duration));
                return tmp.Minutes + ":" + String.Format("{0:00}", tmp.Seconds);
            }
        }

        // Default comparer for Part type.
        public int CompareTo(AudioVK compObj)
        {
            return String.Compare(this.Name, compObj.Name);
        }

        public bool HasLyrics
        {
            get
            {
                return lyrics_id.Trim().Length > 0;
            }
        }


        internal void ChangeAlbum(string id,string token)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.moveToAlbum.xml?" + "audio_ids=" + this.aid + "&album_id=" + id + "&access_token=" + token + "&v=5.21");

            var x = new XmlDocument();
            x.Load(uri.ToString());
        }
    }
}

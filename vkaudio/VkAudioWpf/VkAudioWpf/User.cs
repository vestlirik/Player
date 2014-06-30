using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkAudioWpf
{
    class User
    {

        #region api data
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string sex { get; set; }
        public string bdate { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string photo_50 { get; set; }
        public string photo_100 { get; set; }
        public string photo_200_orig { get; set; }
        public string photo_200 { get; set; }
        public string photo_400_orig { get; set; }
        public string photo_max { get; set; }
        public string photo_max_orig { get; set; }
        public string online { get; set; }
        public string status { get; set; }
        public string status_audio_id { get; set; }
        public string status_audio_owner_id { get; set; }
        public string status_audio_artist { get; set; }
        public string status_audio_title { get; set; }
        public string status_audio_url { get; set; }
        public string last_seen_time { get; set; }
        public string last_seen_platform { get; set; }
        public string last_seen_platform_string
        {
            get
            {
                if (last_seen_platform == "1")
                    return "mobile";
                if (last_seen_platform == "2")
                    return "iphone";
                if (last_seen_platform == "3")
                    return "ipad";
                if (last_seen_platform == "4")
                    return "android";
                if (last_seen_platform == "5")
                    return "wphone";
                if (last_seen_platform == "6")
                    return "windows";
                if (last_seen_platform == "7")
                    return "web";
                else
                    return "";
            }
        }
        public string can_see_audio { get; set; }
        public string can_write_private_message { get; set; }
        #endregion

        private PlayListVk playlist;
        private bool isPlaylistDownloaded = false;
        public bool IsDownloaded
        {
            get
            {
                return isPlaylistDownloaded;
            }
        }

        public User(System.Xml.XmlNode xmlNode)
        {
            this.id = xmlNode["id"].InnerText;
            this.first_name = xmlNode["first_name"].InnerText;
            this.last_name = xmlNode["last_name"].InnerText;
            this.sex = xmlNode["sex"].InnerText;
            try
            {
                this.bdate = xmlNode["bdate"].InnerText;
            }
            catch
            {
                this.bdate = "";
            }
            try
            {
                this.city = xmlNode["city"]["title"].InnerText;
            }
            catch
            {
                this.city = "";
            }
            try
            {
                this.country = xmlNode["country"]["title"].InnerText;
            }
            catch
            {
                this.country = "";
            }
            try
            {
                this.photo_50 = xmlNode["photo_50"].InnerText;
            }
            catch
            {
                this.photo_50 = "";
            }
            try
            {
                this.photo_100 = xmlNode["photo_100"].InnerText;
            }
            catch
            {
                this.photo_100 = "";
            }
            try
            {
                this.photo_400_orig = xmlNode["photo_400_orig"].InnerText;
            }
            catch
            {
                this.photo_400_orig = "";
            }
            try
            {
                this.photo_max = xmlNode["photo_max"].InnerText;
            }
            catch
            {
                this.photo_max = "";
            }
            this.online = xmlNode["online"].InnerText;
            this.status = xmlNode["status"].InnerText;
            try
            {
                this.status_audio_id = xmlNode["status_audio"]["id"].InnerText;
                this.status_audio_owner_id = xmlNode["status_audio"]["owner_id"].InnerText;
                this.status_audio_artist = xmlNode["status_audio"]["artist"].InnerText;
                this.status_audio_title = xmlNode["status_audio"]["title"].InnerText;
                this.status_audio_url = xmlNode["status_audio"]["url"].InnerText;
            }
            catch
            {
                this.status_audio_id = "";
                this.status_audio_owner_id = "";
                this.status_audio_artist = "";
                this.status_audio_title = "";
                this.status_audio_url = "";
            }
            this.last_seen_time = xmlNode["last_seen"]["time"].InnerText;
            this.last_seen_platform = xmlNode["last_seen"]["platform"].InnerText;
            this.can_see_audio = xmlNode["can_see_audio"].InnerText;
            this.can_write_private_message = xmlNode["can_write_private_message"].InnerText;

        }

        internal void DownloadTrackList(string token)
        {
            playlist = new PlayListVk();
            playlist.DownloadTracks(
                new []{
                    this.id,
                    token
                }
                );
            isPlaylistDownloaded = true;
        }

        public PlayListVk GetPlaylist()
        {
            return playlist;
        }


        internal void SetDataFrom(User user)
        {
            this.bdate = user.bdate;
            this.can_see_audio = user.can_see_audio;
            this.can_write_private_message = user.can_write_private_message;
            this.city = user.city;
            this.country = user.country;
            this.first_name = user.first_name;
            this.last_name = user.last_name;
            this.last_seen_platform = user.last_seen_platform;
            this.last_seen_time = user.last_seen_time;
            this.online = user.online;
            this.photo_100 = user.photo_100;
            this.photo_200= user.photo_200;
            this.photo_200_orig = user.photo_200_orig;
            this.photo_400_orig = user.photo_400_orig;
            this.photo_50 = user.photo_50;
            this.photo_max = user.photo_max;
            this.photo_max_orig = user.photo_max_orig;
            this.sex = user.sex;
            this.status = user.status;
            this.status_audio_artist = user.status_audio_artist;
            this.status_audio_id = user.status_audio_id;
            this.status_audio_owner_id = user.status_audio_owner_id;
            this.status_audio_title = user.status_audio_title;
            this.status_audio_url = user.status_audio_url;
        }
    }
}

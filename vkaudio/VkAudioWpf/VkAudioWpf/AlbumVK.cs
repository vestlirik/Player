using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vkAudio;

namespace VkAudioWpf
{
    class AlbumVK
    {
        private bool IsLoaded = false;
        public AlbumVK(string id, string owner_id, string title)
        {
            Id = id;
            Owner_id = owner_id;
            Title = title;
        }

        public AlbumVK(System.Xml.XmlNode xmlNode)
        {
            this.Id = xmlNode["id"].InnerText;
            this.Owner_id = xmlNode["owner_id"].InnerText;
            this.Title = xmlNode["title"].InnerText;

        }

        public string Id { get; set; }
        public string Owner_id { get; set; }
        public string Title { get; set; }

        public PlayListVk playlist;

        public void LoadAlbum(string[] data)
        {
            if (!IsLoaded)
            {
                var userId = data[0];
                var token = data[1];
                var albumId = data[2];
                playlist = new PlayListVk();
                playlist.DownloadAlbumTracks(userId, token, albumId);
                IsLoaded = true;
            }
        }
    }
}

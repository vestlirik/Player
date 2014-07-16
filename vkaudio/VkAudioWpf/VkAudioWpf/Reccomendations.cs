using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace VkAudioWpf
{
    class Reccomendations
    {
        private AudioVK _audio;
        private PlayListVk recommedPlaylist;
        private string vkToken;

        public Reccomendations(string VKToken)
        {
            vkToken = VKToken;
            recommedPlaylist = new PlayListVk();
        }

        public void LoadReccomendations()
        {
            recommedPlaylist.DownloadReccomendations(vkToken);
        }
        public void LoadReccomendationsByAudio(AudioVK audio)
        {
            _audio = audio;
            recommedPlaylist.DownloadReccomendations(vkToken, _audio);
        }


        public PlayListVk Playlist
        {
            get
            {
                return recommedPlaylist;
            }
        }
        internal int GetSearchedCount()
        {
            return recommedPlaylist.Count();
        }

        public string Query
        {
            get
            {
                return _audio.artist+" - "+_audio.title;
            }
        }

    }
}

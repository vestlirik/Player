using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkAudioWpf
{
    class Search
    {
        private string queryString;
        private PlayListVk playlist;
        private string token;
        public Search(string token)
        {
            this.token = token;
            playlist = new PlayListVk();
        }

        public void SearchTracks(string query)
        {
            if (query.Trim() != "")
            {
                queryString = query;
                playlist.DownloadTracksByQuery(token, queryString);
            }
        }

        internal int GetSearchedCount()
        {
            return playlist.Count();
        }

        public string Query
        {
            get
            {
                return queryString;
            }
        }

        public PlayListVk Playlist
        {
            get
            {
                return this.playlist;
            }
        }
    }
}

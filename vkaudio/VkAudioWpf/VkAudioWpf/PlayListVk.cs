using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VkAudioWpf
{
    public class PlayListVk
    {
        List<AudioVK> tracks;
        AudioVK[] tracksTMP;
        bool reverse = false;

        //current selected index of track
        private int selTrack;

        public int SelTrack
        {
            get
            {
                return selTrack;
            }
            set
            {
                selTrack = value;
            }
        }

        public PlayListVk()
        {
            this.tracks = new List<AudioVK>();
        }

        //count of tracks
        public int Count()
        {
            return tracks.Count;
        }

        //return current track
        public AudioVK GetCurrentTrackVK()
        {
            if (SelTrack >= 0 && tracks.Count >= SelTrack && tracks.Count > 0)
                return tracks[SelTrack];
            else
                return null;
        }

        //get tracks from vk user
        public void DownloadTracks(string[] data)
        {

            var userId = data[0];
            var token = data[1];
            Uri uri = new Uri("https://api.vk.com/method/audio.get.xml?owner_id=" + userId + /*"&count=50" + */"&access_token=" + token + "&v=5.9");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var audioElements = x.ChildNodes[1].ChildNodes[1];

            tracks.Clear();

            int length = audioElements.ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var audio = new AudioVK(audioElements.ChildNodes[i]);
                tracks.Add(audio);
            }
        }

        internal void DownloadAlbumTracks(string userId, string token, string albumId)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.get.xml?owner_id=" + userId + "&album_id=" + albumId + "&access_token=" + token + "&v=5.9");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var audioElements = x.ChildNodes[1].ChildNodes[1];

            tracks.Clear();

            int length = audioElements.ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var audio = new AudioVK(audioElements.ChildNodes[i]);
                tracks.Add(audio);
            }
        }
        internal void DownloadTracksByQuery(string token, string queryString)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.search.xml?q=" + queryString + "&count=300" + "&access_token=" + token + "&v=5.9");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var audioElements = x.ChildNodes[1].ChildNodes[1];

            tracks.Clear();

            int length = audioElements.ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var audio = new AudioVK(audioElements.ChildNodes[i]);
                tracks.Add(audio);
            }
        }



        public List<AudioVK> GetTrackListVK()
        {
            return tracks;//.Select(x => x.Name).ToList();
        }

        public void Remove(int selIndex)
        {
            tracks.RemoveAt(selIndex);
        }

        public void RemoveFile(int index, string owner, string token)
        {

            Uri uri = new Uri("https://api.vk.com/method/audio.delete.xml?audio_id=" + tracks[index].aid + "&owner_id=" + owner + "&access_token=" + token);
            var x = new XmlDocument();
            x.Load(uri.ToString());

            tracks.RemoveAt(index);

            if (index < SelTrack)
                SelTrack--;
        }


        //for vk
        public void SetStatus(string audioId, string token)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.setBroadcast.xml?audio=" + audioId + "&access_token=" + token);
            var x = new XmlDocument();
            x.Load(uri.ToString());
        }

        internal string GetTrackN(int i)
        {
            return tracks[i].artist + " - " + tracks[i].title;
        }

        internal void SortByName()
        {
            tracksTMP = new AudioVK[tracks.Count];
            tracks.CopyTo(tracksTMP);
            tracks.Sort();
            if (reverse)
                tracks.Reverse();
        }
        internal void SortByDate()
        {
            if (tracksTMP != null)
            {
                tracks.Clear();
                tracks.AddRange(tracksTMP);
                tracksTMP = null;
                if (reverse)
                    tracks.Reverse();
            }
        }

        internal void ReverseOn()
        {
            tracks.Reverse();
            reverse = true;
        }
        internal void ReverseOff()
        {
            tracks.Reverse();
            reverse = false;
        }

        internal void SelectTrackByAid(string selTrack)
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                if (String.Compare(tracks[i].aid, selTrack) == 0)
                {
                    this.SelTrack = i;
                    break;
                }
            }
        }

        public void Add(AudioVK track)
        {
            tracks.Add(track);
        }

        internal void Clear()
        {
            tracks.Clear();
        }

        internal void DownloadReccomendations(string vkToken)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.getRecommendations.xml?count=300&shuffle=1" + "&access_token=" + vkToken + "&v=5.9");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var audioElements = x.ChildNodes[1].ChildNodes[1];

            tracks.Clear();

            int length = audioElements.ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var audio = new AudioVK(audioElements.ChildNodes[i]);
                tracks.Add(audio);
            }
        }
        internal void DownloadReccomendations(string vkToken, AudioVK selAudio)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.getRecommendations.xml?target_audio=" + selAudio.owner_id + "_" + selAudio.aid + "&count=300&shuffle=1" + "&access_token=" + vkToken + "&v=5.9");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var audioElements = x.ChildNodes[1].ChildNodes[1];

            tracks.Clear();

            int length = audioElements.ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var audio = new AudioVK(audioElements.ChildNodes[i]);
                tracks.Add(audio);
            }
        }

        internal void Edit(AudioVK editedTrack, string trackName, string artistName, string vkToken)
        {
            trackName = fixName(trackName);
            artistName = fixName(artistName);

            Uri uri = new Uri("https://api.vk.com/method/audio.edit.xml?owner_id=" + editedTrack.owner_id + "&audio_id=" + editedTrack.aid + "&artist=" + artistName + "&title=" + trackName + "&access_token=" + vkToken + "&v=5.9");

            var xml = new XmlDocument();
            xml.Load(uri.ToString());
        }

        internal AudioVK GetTrackByIndex(int editedTrack)
        {
            return this.tracks[editedTrack];
        }

        private static String fixName(String pathname)
        {
            Char[] forbiddenSymbols = new Char[] { '<', '>', ':', '\'', '/', '\\', '|', '?', '*' };
            String result = "";
            foreach (Char el in pathname)
            {
                if (!forbiddenSymbols.Contains(el))
                    if (el == '&')
                        result += "%26";
                    else
                        result += el;
            }
            return result;
        }
    }
}

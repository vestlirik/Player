using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace vkAudio
{
    public class PlayListVk : PlayList
    {
        protected new List<AudioVK> tracks;

        public PlayListVk()
        {
            this.tracks = new List<AudioVK>();
        }

        //count of tracks
        public override int Count()
        {
            return tracks.Count;
        }

        //return current track
        public override Audio GetCurrentTrack()
        {
            if (SelTrack >= 0)
            return tracks[SelTrack];
            else
                return null;
        }

        //return current track
        public AudioVK GetCurrentTrackVK()
        {
            if (SelTrack >= 0)
            return tracks[SelTrack];
            else
                return null;
        }

        //get tracks from vk user
        public override void DownloadTracks(string[] data)
        {

            var userId = data[0];
            var token = data[1];
            Uri uri = new Uri("https://api.vk.com/method/audio.get.xml?owner_id=" + userId + /*"&count=50" + */"&access_token=" + token);

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var audioElements = x.GetElementsByTagName("response")[0];

            tracks.Clear();

            int length = audioElements.ChildNodes.Count;

            for (int i = 1; i < length; i++)
            {
                var audio = new AudioVK(audioElements.ChildNodes[i]);
                tracks.Add(audio);
            }
        }


        public override List<string> GetTrackList()
        {
            return tracks.Select(x => x.Name).ToList();
        }

        public override void Remove(int selIndex)
        {
            tracks.RemoveAt(selIndex);
        }

        public override void RemoveFile(int selIndex)
        {
            throw new NotImplementedException();
        }
    }
}

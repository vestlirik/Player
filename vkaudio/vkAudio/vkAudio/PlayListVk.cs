using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace vkAudio
{
    public class PlayListVk:PlayList
    {
        protected new List<AudioVK> tracks;

        public PlayListVk()
        {
            this.tracks = new List<AudioVK>();
        }

        public override int Count()
        {
                return tracks.Count;
        }
        
        public override Audio GetCurrentTrack()
        {
            return tracks[SelTrack];
        }
        public AudioVK GetCurrentTrackVK()
        {
            return tracks[SelTrack];
        }
        
        public override void DownloadTracks(string[] data)
        {

            var userId = data[0];
            var token = data[1];
            Uri uri = new Uri("https://api.vk.com/method/audio.get.xml?owner_id=" + userId + "&count=50" + "&access_token=" + token);

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
            return tracks.Select(x=>x.Name).ToList();
        }
    }
}

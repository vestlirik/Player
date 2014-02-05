using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkAudio
{
    public class PlayListLocal:PlayList
    {
        protected new List<AudioLocal> tracks;

        public PlayListLocal()
        {
            tracks = new List<AudioLocal>();
        }

        public override int Count()
        {
            return tracks.Count;
        }

        public override Audio GetCurrentTrack()
        {
            return tracks[SelTrack];
        }
        
        public override void DownloadTracks(string[] path)
        {
            var folder = path[0];
            tracks.Clear();
            List<string> tmpList = new List<string>();
            GetFilesFromDirectory(folder, tmpList);
            foreach(var str in tmpList)
            {
                AudioLocal audio = new AudioLocal(str);
                tracks.Add(audio);
            }
        }

        public void GetFilesFromDirectory(string pth,List<string> tmp)
        {
            try
            {
                if (pth.IndexOf(":\\$RECYCLE.BIN") != 1)
                {
                    var dirs = Directory.GetDirectories(pth);
                    var dir = Directory.GetFiles(pth, "*.mp3");
                    tmp.AddRange(dir);
                    foreach (var elm in dirs)
                    {
                        GetFilesFromDirectory(elm,tmp);
                    }
                }
            }
            catch { }
        }

        public override List<string> GetTrackList()
        {
            return tracks.Select(x=>x.Name).ToList();
        }
    }
}

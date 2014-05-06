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

        //get tracks from path
        public override void DownloadTracks(string[] path)
        {
            var folder = path[0];
            tracks.Clear();
            //list with track paths
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

        //get list of track names, perhaps for listbox
        public override List<string> GetTrackList()
        {
            return tracks.Select(x=>x.Name).ToList();
        }

        public override void Remove(int selIndex)
        {
            tracks.RemoveAt(selIndex);
        }

        public override void RemoveFile(int selIndex)
        {
            var obj = tracks[selIndex];
            var file = new FileInfo(obj.GetLocation);
            if(file.Exists)
                file.Delete();
            Remove(selIndex);
        }
    }
}

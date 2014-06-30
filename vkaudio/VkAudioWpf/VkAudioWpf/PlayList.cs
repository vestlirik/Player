using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkAudioWpf
{
    public abstract class PlayList
    {
        protected List<Audio> tracks;

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



        public PlayList()
        {
            selTrack=-1;
        }

        //count of tracks
        public abstract int Count();

        //return current track
        public abstract Audio GetCurrentTrack();

        //get tracks from address
        public abstract void DownloadTracks(string[] data);

        //get list of track names, perhaps for listbox
        public abstract List<string> GetTrackList();

        public abstract void Remove(int selIndex);
    }
}

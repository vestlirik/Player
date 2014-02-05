using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkAudio
{
    public abstract class PlayList
    {
        protected List<Audio> tracks;

        private int selTrack;

        public int SelTrack
        {
            get
            {
                if (selTrack < 0 && Count() > 0)
                    selTrack = 0;
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

        public abstract int Count();

        public abstract Audio GetCurrentTrack();

        public abstract void DownloadTracks(string[] data);

        public abstract List<string> GetTrackList();
    }
}

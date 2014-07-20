
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace vkAudio
{
    //status of player
    public enum PLAYER_STATUS
    {
        PLAYER_STATUS_NOT_READY,
        PLAYER_STATUS_READY_STOPPED,
        PLAYER_STATUS_PLAYING,
        PLAYER_STATUS_PAUSED,
        PLAYER_STATUS_ENDED,
    };

    public class Player
    {
        //Current status of the player
        PLAYER_STATUS curStatus;

        //referance of current stream
        int stream;

        //Delegate to let the clients know about Event change
        public delegate void OnStatusUpdate(PLAYER_STATUS status);

        //Event for clients to subscribe to, if they want to get notfied.
        public event OnStatusUpdate StatusChanged;

        private float volumeState = 1f;


        public Player()
        {
            BassNet.Registration("vestlirik@ukr.net","2X1723201782018");
            //engine initialization
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);

            //set default status
            curStatus = PLAYER_STATUS.PLAYER_STATUS_NOT_READY;
        }

        //This will be the first step if we want to play something
        public void AttachSong(String url)
        {
            if (url != null)
            {
                //destroy current playing stream
                Bass.BASS_Free();
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);
                stream = Bass.BASS_StreamCreateFile(url, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);

                // pre-buffer
                Bass.BASS_ChannelUpdate(stream, 0);

                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }
        //attach from internet
        public void AttachUrlSong(String url)
        {
            if (url != null)
            {
                //destroy current playing stream
                Bass.BASS_Free();
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);
                stream = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_SAMPLE_FLOAT,null,IntPtr.Zero);
                
                // pre-buffer
                Bass.BASS_ChannelUpdate(stream, 0);

                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }

        //Play playback
        public void Play()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_PLAYING)
            {
                Bass.BASS_ChannelPlay(stream, false);
                ChangeVolume(volumeState);
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PLAYING);
            }
        }

        //Stop playback
        public void Stop()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED)
            {

                Bass.BASS_ChannelStop(stream);
                Bass.BASS_Free();
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }

        //Pause playback
        public void Pause()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_PAUSED)
            {

                Bass.BASS_ChannelPause(stream);
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PAUSED);
            }
        }

        //Resume playback
        public void Resume()
        {
            if (curStatus == PLAYER_STATUS.PLAYER_STATUS_PAUSED)
            {
                Bass.BASS_ChannelPlay(stream,false);
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PLAYING);
            }
        }

        ~Player()
        {
            Bass.BASS_Free();
        }

        //UI can call this function to keep itself up to date
        public PLAYER_STATUS GetPlayerstatus()
        {
            return curStatus;
        }
        
        //Duration of current song
        public int Duration(string path)
        {
                // length in bytes 
                long len = Bass.BASS_ChannelGetLength(stream);
                // the time length 
                double time = Bass.BASS_ChannelBytes2Seconds(stream, len);

                return (int)time;
        }
        
        //Human readable duration string
        public string DurationString(string path)
        {
            // length in bytes 
            long len = Bass.BASS_ChannelGetLength(stream);
            // the time length 
            double time = Bass.BASS_ChannelBytes2Seconds(stream, len);

            return TimeSpan.FromSeconds(time).Minutes + ":" + String.Format("{0:00}", TimeSpan.FromSeconds(time).Seconds);
        }
        
        //Current play position
        public double CurruntPosition
        {
            get
            {
                // length in bytes 
                long len = Bass.BASS_ChannelGetPosition(stream);
                // the time length 
                return Bass.BASS_ChannelBytes2Seconds(stream, len);
            }
            set //Set current position, perhaps from seekbar of UI
            {
                Bass.BASS_ChannelSetPosition(stream,value);
            }
        }
        
        //Human readable current play position
        public string CurruntPositionString
        {
            get
            {
                var z = TimeSpan.FromSeconds(CurruntPosition);
                return z.Minutes + ":" + String.Format("{0:00}",z.Seconds);
            }
        }
        
        //Update the UI with new status of player
        private void UpdateStatus(PLAYER_STATUS status)
        {
            curStatus = status;
            StatusChanged(curStatus);
        }

        //System volume
        public int Volume
        {
            get
            {
                return (int)(Bass.BASS_GetVolume() * 100);
            }
            set
            {
                Bass.BASS_SetVolume((float)value / 100);
            }
        }
        
        private string toUtf8(string unknown)
        {
            return new string(unknown.ToCharArray().
                Select(x => ((x + 848) >= 'А' && (x + 848) <= 'ё') ? (char)(x + 848) : x).
                ToArray());
        }


        internal void ChangeVolume(double p)
        {

            Bass.BASS_ChannelSetAttribute(stream, BASSAttribute.BASS_ATTRIB_VOL, (float)p);
            volumeState = (float)p;
        }


        bool mute = false;
        float prevVolumeLevel = 0;

        internal void Mute()
        {
 	        if(!mute)
            {
                prevVolumeLevel = volumeState;
                volumeState = 0f;
                mute = true;
            }
            else
            {
                volumeState = prevVolumeLevel;
                mute = false;
            }
        }



        internal double GetDowloadedPercentage()
        {
            float progress;
            // file length 
            long len = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_END);
            // download progress 
            long down = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
            // get channel info
            BASS_CHANNELINFO info = Bass.BASS_ChannelGetInfo(stream);
            // streaming in blocks? 
            if (BASSFlag.BASS_STREAM_BLOCK != BASSFlag.BASS_DEFAULT)
            {
                // percentage of buffer used
                progress = (down) * 100f / len;
                if (progress > 100)
                    progress = 100; // restrict to 100 (can be higher)
            }
            else
            {
                // percentage of file downloaded
                progress = down * 100f / len;
            }
            return progress;
        }
    }
}

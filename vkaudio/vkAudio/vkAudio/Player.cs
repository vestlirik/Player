
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

        //Main player(engine)
        int stream;

        bool fromNetwork = false;

        //Delegate to let the clients know about Event change
        public delegate void OnStatusUpdate(PLAYER_STATUS status);

        //Event for clients to subscribe to, if they want to get notfied.
        public event OnStatusUpdate StatusChanged;


        public Player()
        {
            //create engine
            Bass.BASS_Free();

            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);

            //default settings
            curStatus = PLAYER_STATUS.PLAYER_STATUS_NOT_READY;
        }

        //void waveOutDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        //{
        //    if (Repeat)
        //    {
        //        try
        //        {
        //            CurruntPosition = - (int)Duration - 1;
        //        }
        //        catch
        //        {
        //            CurruntPosition = -(int)Duration;
        //        }
        //        Pause();
        //        Resume();
        //    }
        //    else
        //    {
        //        Stop();
        //        UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_ENDED);
        //    }
        //}

        public bool Repeat = false;

        //This will be the first step if we want to play something
        public void AttachSong(String url)
        {
            if (url != null)
            {
                Bass.BASS_Free();

                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);

                //fromNetwork = false;

                stream = Bass.BASS_StreamCreateFile(url, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);

                // pre-buffer
                Bass.BASS_ChannelUpdate(stream, 0);


                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }

        public void AttachUrlSong(String url)
        {
            if (url != null)
            {
                Bass.BASS_Free();
                //Bass.

                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);

                //fromNetwork = false;

                stream = Bass.BASS_StreamCreateURL(url, 0, BASSFlag.BASS_SAMPLE_FLOAT,null,IntPtr.Zero);
                
                // pre-buffer
                Bass.BASS_ChannelUpdate(stream, 0);

                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }

        
        public void Play()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_PLAYING)
            {
                Bass.BASS_ChannelPlay(stream, false);
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PLAYING);
            }
        }

        //Stop playback
        public void Stop()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED)
            {

                Bass.BASS_ChannelStop(stream);
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
        
        //Duration of current item
        public int Duration(string path)
        {
                // length in bytes 
                long len = Bass.BASS_ChannelGetLength(stream);
                // the time length 
                double time = Bass.BASS_ChannelBytes2Seconds(stream, len);

                return (int)time;
        }
        
        ////HUman readable duration string
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

        public double Fraquency(string str)
        {
            TagLib.File file = TagLib.File.Create(str);
            return file.Properties.AudioSampleRate;
        }
        
        public int Bitrate(string str)
        {
            TagLib.File file = TagLib.File.Create(str);
            return file.Properties.AudioBitrate;
        }
    }
}

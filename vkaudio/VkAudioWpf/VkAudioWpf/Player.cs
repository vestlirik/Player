
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Un4seen.Bass;
using VkAudioWpf;

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

    public sealed class Player:IDisposable
    {
        //Current status of the player
        PLAYER_STATUS curStatus;

        //referance of current stream
        int stream;

        //Delegate to let the clients know about Event change
        public delegate void OnStatusUpdate(PLAYER_STATUS status,EventArgs e);

        //Event for clients to subscribe to, if they want to get notfied.
        public event OnStatusUpdate StatusChanged;

        private float volumeState = 1f;

        private FileStream _fs = null;
        private DOWNLOADPROC _myDownloadProc;
        private byte[] _data; // local data buffer
        private AudioVK currTrack;


        private const string FOLDER_PATH = "cache";

        public string FOLDER
        {
            get { return FOLDER_PATH; }
        } 


        private bool isLocal = false;


        public Player()
        {
            BassNet.Registration("vestlirik@ukr.net", "2X1723201782018");
            //engine initialization
            Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);

            //set default status
            curStatus = PLAYER_STATUS.PLAYER_STATUS_NOT_READY;

        }

       
        public void AttachTrack(AudioVK track)
        {
            Bass.BASS_Free();
            Settings.CheckCurrFolder(FOLDER_PATH);
            if (_fs != null)
            {
                _fs.Close();
                _fs = null;
            }
            string filepath = Directory.GetCurrentDirectory()+"\\"+FOLDER_PATH + "\\" + track.aid+".mp3";
            if (File.Exists(filepath))
                if (int.Parse(GetDuration(filepath)) >= (int.Parse(track.duration) - 1) && int.Parse(GetDuration(filepath)) <= (int.Parse(track.duration) + 1))
                AttachSong(filepath);
                else
                AttachUrlSong(track);
            else
                AttachUrlSong(track);
        }


        private void AttachSong(String url)
        {
            if (url != null)
            {
                isLocal = true;
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
        private void AttachUrlSong(AudioVK track)
        {
            if (track != null)
            {
                isLocal = false;
                currTrack = track;
                _myDownloadProc = new DOWNLOADPROC(MyDownload);
                //destroy current playing stream
                Bass.BASS_Free();
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);
                stream = Bass.BASS_StreamCreateURL(track.GetLocation, 0, BASSFlag.BASS_DEFAULT, _myDownloadProc, IntPtr.Zero);

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
                Bass.BASS_ChannelPlay(stream, false);
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PLAYING);
            }
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
                Bass.BASS_ChannelSetPosition(stream, value);
            }
        }

        //Human readable current play position
        public string CurruntPositionString
        {
            get
            {
                var z = TimeSpan.FromSeconds(CurruntPosition);
                return z.Minutes + ":" + String.Format("{0:00}", z.Seconds);
            }
        }

        //Update the UI with new status of player
        private void UpdateStatus(PLAYER_STATUS status)
        {
            curStatus = status;
            StatusChanged(curStatus,null);
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
            if (!mute)
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
            if (!isLocal)
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
            else
                return 100f;
        }
        

        private void MyDownload(IntPtr buffer, int length, IntPtr user)
        {
            if (_fs == null)
            {
                // create the file
                _fs = File.OpenWrite(FOLDER_PATH + "\\" + currTrack.aid + ".mp3");
            }
            if (buffer == IntPtr.Zero)
            {
                // finished downloading
                _fs.Flush();
                _fs.Close();
            }
            else
            {
                // increase the data buffer as needed 
                if (_data == null || _data.Length < length)
                    _data = new byte[length];
                // copy from managed to unmanaged memory
                Marshal.Copy(buffer, _data, 0, length);
                // write to file
                _fs.Write(_data, 0, length);
            }
        }

        
        public string GetDuration(string path)
        {
            string file = path;
            ShellFile so = ShellFile.FromFilePath(file);
            double nanoseconds;
            double.TryParse(so.Properties.System.Media.Duration.Value.ToString(), out nanoseconds);
            if (nanoseconds > 0)
            {
                double seconds = Convert100NanosecondsToMilliseconds(nanoseconds) / 1000;
                return ((int)seconds).ToString();
            }
            else
            return "0";
        }

        public static double Convert100NanosecondsToMilliseconds(double nanoseconds)
        {
            // One million nanoseconds in 1 millisecond, 
            // but we are passing in 100ns units...
            return nanoseconds * 0.0001;
        }


        public void Dispose(bool b)
        {
            if (b)
                GC.SuppressFinalize(currTrack);

            Bass.BASS_Free();
            stream = 0;
        }

        public void Dispose()
        {
            Bass.BASS_Free();
            stream = 0;
        }

        internal int GetStream()
        {
            return stream;
        }
    }
}

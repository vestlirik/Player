
using System.Windows;
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
using NAudio.Wave;
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

    public sealed class Player : IDisposable
    {
        IWavePlayer waveOutDevice;
        AudioFileReader audioFileReader;
        private WaveStream blockAlignedStream;
        private Stream ms;

        //Current status of the player
        PLAYER_STATUS curStatus;

        //Delegate to let the clients know about Event change
        public delegate void OnStatusUpdate(PLAYER_STATUS status, EventArgs e);

        //Event for clients to subscribe to, if they want to get notfied.
        public event OnStatusUpdate StatusChanged;

        private float volumeState = 1f;

        private FileStream _fs = null;
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
            //engine initialization

            //set default status
            curStatus = PLAYER_STATUS.PLAYER_STATUS_NOT_READY;

        }


        public void AttachTrack(AudioVK track, bool needCache)
        {
            if (needCache)
            {
                Settings.CheckCurrFolder(FOLDER_PATH);
                if (_fs != null)
                {
                    _fs.Close();
                    _fs = null;
                }
                string filepath = Directory.GetCurrentDirectory() + "\\" + FOLDER_PATH + "\\" + track.aid + ".wav";
                if (File.Exists(filepath))
                    if (int.Parse(GetDuration(filepath)) >= (int.Parse(track.duration) - 1) && int.Parse(GetDuration(filepath)) <= (int.Parse(track.duration) + 1) && CheckSize(track.GetLocation, filepath))
                        AttachSong(filepath);
                    else
                        AttachUrlSong(track, needCache);
                else
                    AttachUrlSong(track, needCache);
            }
            else
                AttachUrlSong(track, needCache);
        }

        private bool CheckSize(string uri, string filepath)
        {
            var rq = WebRequest.Create(uri);
            rq.Method = "HEAD";
            var resp = (HttpWebResponse)rq.GetResponse();
            var length = resp.ContentLength;
            return length == new FileInfo(filepath).Length;
        }

        private void AttachSong(String url)
        {
            if (url != null)
            {
                isLocal = true;
                //destroy current playing stream
                audioFileReader = new AudioFileReader(url);

                // pre-buffer
                if (waveOutDevice != null)
                {
                    waveOutDevice.Stop();
                    waveOutDevice.Dispose();
                }
                waveOutDevice = new WaveOut(WaveCallbackInfo.FunctionCallback());
                waveOutDevice.Init(audioFileReader);
                waveOutDevice.Play();

                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }
        //attach from internet
        private void AttachUrlSong(AudioVK track, bool needCache)
        {
            if (track != null)
            {
                isLocal = false;
                currTrack = track;
                ms = new MemoryStream();

                new Thread(delegate(object o)
                {
                    var response = WebRequest.Create(track.GetLocation).GetResponse();
                    using (var stream = response.GetResponseStream())
                    {
                        byte[] buffer = new byte[65536]; // 64KB chunks
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            var pos = ms.Position;
                            ms.Position = ms.Length;
                            ms.Write(buffer, 0, read);
                            ms.Position = pos;
                            if (ms.Position == ms.Length)
                            {
                                var mss = new MemoryStream();
                                ms.CopyTo(mss);
                                mss.Position = 0;

                                using (var mp3Stream = new Mp3FileReader(mss))
                                {
                                    string filepath = Directory.GetCurrentDirectory() + "\\" + FOLDER_PATH + "\\" + track.aid + ".wav";
                                    WaveFileWriter.CreateWaveFile(filepath, mp3Stream);
                                }
                            }
                        }
                        
                        
                    }
                }).Start();


                while (ms.Length < 65536 * 10)
                    Thread.Sleep(100);


                ms.Position = 0;

                blockAlignedStream =
                    new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(ms)));
                if (waveOutDevice != null)
                {
                    waveOutDevice.Stop();
                    waveOutDevice.Dispose();
                }
                waveOutDevice = new WaveOut(WaveCallbackInfo.FunctionCallback());
                waveOutDevice.Init(blockAlignedStream);
                waveOutDevice.Play();

                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }

        //Play playback
        public void Play()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_PLAYING)
            {
                waveOutDevice.Play();
                ChangeVolume(volumeState);
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PLAYING);
            }
        }

        //Stop playback
        public void Stop()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED)
            {
                waveOutDevice.Stop();
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED);
            }
        }

        //Pause playback
        public void Pause()
        {
            if (curStatus != PLAYER_STATUS.PLAYER_STATUS_PAUSED)
            {

                waveOutDevice.Pause();
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PAUSED);
            }
        }

        //Resume playback
        public void Resume()
        {
            if (curStatus == PLAYER_STATUS.PLAYER_STATUS_PAUSED)
            {
                waveOutDevice.Play();
                UpdateStatus(PLAYER_STATUS.PLAYER_STATUS_PLAYING);
            }
        }

        //UI can call this function to keep itself up to date
        public PLAYER_STATUS GetPlayerstatus()
        {
            return curStatus;
        }


        //Current play position
        public double CurruntPosition
        {
            get
            {
                //the time length 
                return blockAlignedStream.CurrentTime.TotalSeconds;
            }
            set //Set current position, perhaps from seekbar of UI
            {
                blockAlignedStream.CurrentTime = TimeSpan.FromSeconds(value);
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
            StatusChanged(curStatus, null);
        }

        //System volume
        public int Volume
        {
            get
            {
                return (int)(waveOutDevice.Volume * 100);
            }
            set
            {
                waveOutDevice.Volume = ((float)value / 100);
                MessageBox.Show(value.ToString());
            }
        }


        internal void ChangeVolume(double p)
        {
            if (waveOutDevice != null)
                waveOutDevice.Volume = (float)p;
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
                long len = blockAlignedStream.Length;
                // download progress 
                long down = blockAlignedStream.Position;

                // percentage of buffer used
                progress = (down) * 100f / len;
                if (progress > 100)
                    progress = 100; // restrict to 100 (can be higher)\
                return progress;
            }

            return 100f;
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

            Dispose();
        }

        public void Dispose()
        {
            waveOutDevice.Stop();
            waveOutDevice.Dispose();
        }

    }
}

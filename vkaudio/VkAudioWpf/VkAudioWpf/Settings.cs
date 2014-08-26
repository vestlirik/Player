using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Un4seen.Bass;

namespace VkAudioWpf
{
    [Serializable]
    public class Settings:ISerializable
    {
        public string LastSessionKey = "";

        public string VKToken = "";
        public string UserId = "";

        public bool EnableVKBroadcast = true;
        public bool EnableRepeating = false;
        public bool EnableShuffling = false;
        public bool EnableViewPlaylist = true;

        public double VolumeLevel = 1;

        private int _stream;
        private int _handle;

        private EqualizerSettings eqalizer;


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");
            info.AddValue("LastSess", LastSessionKey);
            info.AddValue("VKToken", VKToken);
            info.AddValue("VKId", UserId);
            info.AddValue("EnableVKBroadcast", EnableVKBroadcast);
            info.AddValue("EnableRepeating", EnableRepeating);
            info.AddValue("EnableShuffling", EnableShuffling);
            info.AddValue("EnableViewPlaylist", EnableViewPlaylist);
            info.AddValue("VolumeLevel", VolumeLevel);
            for (int i = 0; i < 12; i++)
            info.AddValue("EQ"+i, eqalizer._vlEQ[i]);

        }

        internal float[] GetEQValues()
        {
            return eqalizer.GetValues();
        }

        public Settings(SerializationInfo info, StreamingContext context)
        {
            if (eqalizer == null)
                eqalizer = new EqualizerSettings();
            LastSessionKey = (string)info.GetValue("LastSess",typeof(string));
            VKToken = (string)info.GetValue("VKToken", typeof(string));
            UserId = (string)info.GetValue("VKId", typeof(string));
            EnableVKBroadcast = (bool)info.GetValue("EnableVKBroadcast", typeof(bool));
            EnableRepeating = (bool)info.GetValue("EnableRepeating", typeof(bool));
            EnableShuffling = (bool)info.GetValue("EnableShuffling", typeof(bool));
            EnableViewPlaylist = (bool)info.GetValue("EnableViewPlaylist", typeof(bool));
            VolumeLevel = (double)info.GetValue("VolumeLevel", typeof(double));
            for (int i = 0; i < 12; i++)
                eqalizer._vlEQ[i] = (float)info.GetValue("EQ"+i, typeof(float));
        }

        public Settings()
        {
            if (eqalizer==null)
            eqalizer = new EqualizerSettings();
        }

        public void ChangeStream(int stream)
        {
            _stream = stream;
            _handle = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            EqualizerApplyForNewTrack();
        }

        public void ChangeEqValue(int band,float value)
        {
            eqalizer.ChangeValue(band, value);
            UpdateEQ(band);
        }

        private void UpdateEQ(int band)
        {
            BASS_DX8_PARAMEQ eq = new BASS_DX8_PARAMEQ();
            if (Bass.BASS_FXGetParameters(eqalizer._fxEQ[band], eq))
            {
                eq.fGain = eqalizer.GetValueByIndex(band);
                Bass.BASS_FXSetParameters(eqalizer._fxEQ[band], eq);
            }
        }

        public void EqualizerApplyForNewTrack()
        {
            BASS_DX8_PARAMEQ eq = new BASS_DX8_PARAMEQ();
            for (int i = 0; i < 12; i++)
            {
                eqalizer._fxEQ[i] = Bass.BASS_ChannelSetFX(_stream, BASSFXType.BASS_FX_DX8_PARAMEQ, 0);
            }
            eq.fBandwidth = 2.5f;

            for (int i = 0; i < 12; i++)
            {
                eq.fCenter = eqalizer.GetFraquencyByIndex(0);
                eq.fGain = eqalizer.GetValueByIndex(0);
                Bass.BASS_FXSetParameters(eqalizer._fxEQ[0], eq);
            }

        }

        public string SetOffline()
        {
            Uri uri = new Uri("https://api.vk.com/method/account.setOffline.xml?access_token=" + this.VKToken);
            var x = new XmlDocument();
            x.Load(uri.ToString());
            return x.ChildNodes[1].InnerText;
        }

        public static string UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            if (dtDateTime.Date == DateTime.Now.Date)
                return "сьогодні " + dtDateTime.ToLongTimeString();
            if (dtDateTime.Date == DateTime.Now.Date.AddDays(-1))
                return "вчора " + dtDateTime.ToLongTimeString();
            return dtDateTime.ToString();
        }

        public static void CheckCurrFolder(string FOLDER_PATH)
        {
            string path = Directory.GetCurrentDirectory();
            path += "\\" + FOLDER_PATH;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void DownloadRemoteImageFile(string uri, string fileName)
        {
            CheckCurrFolder("images");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Check that the remote file was found. The ContentType
            // check is performed since a request for a non-existent
            // image file might be redirected to a 404-page, which would
            // yield the StatusCode "OK", even though the image was not
            // found.
            if ((response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Moved ||
                response.StatusCode == HttpStatusCode.Redirect) &&
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            {

                // if the remote file was found, download oit
                using (Stream inputStream = response.GetResponseStream())
                using (Stream outputStream = File.OpenWrite("images"+"\\"+fileName))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    do
                    {
                        bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, bytesRead);
                    } while (bytesRead != 0);
                }
            }
        }
        
        public static string CheckImage(string name)
        {
            string path = Directory.GetCurrentDirectory();
            path += "\\" + "images"+"\\"+name;
            if (File.Exists(path))
            {
                return path;
            }
            return "";
        }
    }
}

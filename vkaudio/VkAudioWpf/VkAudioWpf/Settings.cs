using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VkAudioWpf
{
    [Serializable]
    class Settings:ISerializable
    {
        public string LastSessionKey = "";

        public string VKToken = "";
        public string UserId = "";

        public bool EnableVKBroadcast = true;
        public bool EnableRepeating = false;
        public bool EnableShuffling = false;
        public bool EnableViewPlaylist = true;

        public double VolumeLevel = 1;


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

        }

        public Settings(SerializationInfo info, StreamingContext context)
        {
            LastSessionKey = (string)info.GetValue("LastSess",typeof(string));
            VKToken = (string)info.GetValue("VKToken", typeof(string));
            UserId = (string)info.GetValue("VKId", typeof(string));
            EnableVKBroadcast = (bool)info.GetValue("EnableVKBroadcast", typeof(bool));
            EnableRepeating = (bool)info.GetValue("EnableRepeating", typeof(bool));
            EnableShuffling = (bool)info.GetValue("EnableShuffling", typeof(bool));
            EnableViewPlaylist = (bool)info.GetValue("EnableViewPlaylist", typeof(bool));
            VolumeLevel = (double)info.GetValue("VolumeLevel", typeof(double));
        }

        public Settings()
        {

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

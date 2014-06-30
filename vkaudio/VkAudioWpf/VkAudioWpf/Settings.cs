using System;
using System.Collections.Generic;
using System.Linq;
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new System.ArgumentNullException("info");
            info.AddValue("LastSess", LastSessionKey);
            info.AddValue("VKToken", VKToken);
            info.AddValue("VKId", UserId);
        }

        public Settings(SerializationInfo info, StreamingContext context)
        {
            LastSessionKey = (string)info.GetValue("LastSess",
               typeof(string));
            VKToken = (string)info.GetValue("VKToken",
               typeof(string));
            UserId = (string)info.GetValue("VKId",
               typeof(string));
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
    }
}

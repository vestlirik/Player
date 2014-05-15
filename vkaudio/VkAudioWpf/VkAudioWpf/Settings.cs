using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
    }
}

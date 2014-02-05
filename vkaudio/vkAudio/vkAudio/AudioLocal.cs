using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkAudio
{
    public class AudioLocal:Audio
    {
        public AudioLocal(string pth)
        {
            this.URL = pth;
            this.Name = pth.Substring(pth.LastIndexOf("\\") + 1, pth.LastIndexOf(".mp3") - pth.LastIndexOf("\\") - 1);
        }

    }
}

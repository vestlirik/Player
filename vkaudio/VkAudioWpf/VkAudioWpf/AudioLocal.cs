using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VkAudioWpf
{
    public class AudioLocal:Audio
    {
        public AudioLocal(string pth)
        {
            this.URL = pth;
            //human view of name
            try
            {
                this.Name = pth.Substring(pth.LastIndexOf("\\") + 1, pth.LastIndexOf(".mp3") - pth.LastIndexOf("\\") - 1);
            }
            catch
            {
                this.Name = pth;
            }
        }

    }
}

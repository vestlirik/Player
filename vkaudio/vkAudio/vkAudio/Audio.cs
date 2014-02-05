using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkAudio
{
    public class Audio
    {
        protected string URL;

        public string GetLocation
        {
            get
            {
                return URL;
            }
        }

        public string Name { get; protected set; }
    }
}

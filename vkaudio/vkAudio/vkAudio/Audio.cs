using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vkAudio
{
    public class Audio
    {
        //location of file
        protected string URL;

        public string GetLocation
        {
            get
            {
                return URL;
            }
        }

        //human view of name
        public string Name { get; protected set; }
    }
}

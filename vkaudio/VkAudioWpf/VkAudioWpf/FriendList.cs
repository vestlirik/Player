using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VkAudioWpf
{
    class FriendList
    {
        protected List<User> users;

        public FriendList()
        {
            users = new List<User>();
        }

        public int Count()
        {
            return users.Count;
        }

        public void DownloadUsers(string userId)
        {

            Uri uri = new Uri("https://api.vk.com/method/friends.get.xml?user_id=" + userId + "&order=hints&fields=sex, bdate, city, country,  photo_50, photo_max_orig, online, last_seen, status, counters, can_write_private_message, can_see_audio" + "&v=5.21");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var receivedUsers = x.GetElementsByTagName("response")[0];

            users.Clear();

            int length = receivedUsers.ChildNodes[1].ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var user = new User(receivedUsers.ChildNodes[1].ChildNodes[i]);
                users.Add(user);
            }

            users = users.OrderByDescending(us => int.Parse(us.online)).ToList();
            users = users.OrderByDescending(us => us.status_audio_id!="").ToList();
        }

        public User[] GetUsers()
        {
            return users.ToArray();
        }
    }
}

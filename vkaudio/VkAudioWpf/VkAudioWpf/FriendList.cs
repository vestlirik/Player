using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace VkAudioWpf
{
    class FriendList
    {
        protected List<User> users;
        private User selectedUser;
        private string userId;
        private string access_token;
        private Timer timer;


        public delegate void MethodContainer();

        //Событие OnCount c типом делегата MethodContainer.
        public event MethodContainer OnUsersUpdated;

        public FriendList(string _userId, string _access_token)
        {
            users = new List<User>();
            selectedUser = null;
            userId=_userId;
            access_token = _access_token;

            DownloadUsers();

            //timer
            timer = new Timer();
            timer.Interval = 10000;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            UpdateUsers();
            OnUsersUpdated();
        }

        public int Count()
        {
            return users.Count;
        }

        public void DownloadUsers()
        {

            Uri uri = new Uri("https://api.vk.com/method/friends.get.xml?user_id=" + userId + "&order=hints&fields=sex, bdate, city, country,  photo_50, photo_max_orig, online, last_seen, status, counters, can_write_private_message, can_see_audio" + "&access_token=" + access_token + "&v=5.21");

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
            users = users.OrderByDescending(us => us.status_audio_id != "").ToList();
        }

        public void UpdateUsers()
        {
            Uri uri = new Uri("https://api.vk.com/method/friends.get.xml?user_id=" + userId + "&order=hints&fields=sex, bdate, city, country,  photo_50, photo_max_orig, online, last_seen, status, counters, can_write_private_message, can_see_audio" + "&access_token=" + access_token + "&v=5.21");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var receivedUsers = x.GetElementsByTagName("response")[0];

            //users.Clear();

            int length = receivedUsers.ChildNodes[1].ChildNodes.Count;
            List<User> tmpUsers = new List<User>();
            for (int i = 0; i < length; i++)
            {
                var user = new User(receivedUsers.ChildNodes[1].ChildNodes[i]);
                var tmpUser = users.FirstOrDefault(z => z.id == user.id);
                if (tmpUser != null)
                    tmpUser.SetDataFrom(user);
                else
                    users.Add(tmpUser);

                tmpUsers.Add(tmpUser);
            }

            users=users.OrderBy(d=>tmpUsers.IndexOf(d)).ToList();

            users = users.OrderByDescending(us => int.Parse(us.online)).ToList();
            users = users.OrderByDescending(us => us.status_audio_id != "").ToList();
        }


        public User[] GetUsers()
        {
            return users.ToArray();
        }

        internal void SelectUser(int index)
        {
            if (index >= 0)
            {
                if (CanGetTracks(index))
                {
                    selectedUser = users[index];
                    if (!selectedUser.IsDownloaded)
                        selectedUser.DownloadTrackList(access_token);
                }
            }
        }
        internal void UpdateUserTracks()
        {
            if(selectedUser!=null)
                if (this.CanGetTracks())
                    selectedUser.DownloadTrackList(access_token);
        }

        public bool CanGetTracks()
        {
            if (selectedUser != null)
                return this.selectedUser.can_see_audio == "1";
            else
                return false;
        }
        public bool CanGetTracks(int index)
        {
            if (users != null)
                return this.users[index].can_see_audio == "1";
            else
                return false;
        }


        internal PlayListVk GetSelectedUserPlaylist()
        {
            return selectedUser.GetPlaylist();
        }


        internal string GetCurrentUserName()
        {
            return selectedUser.first_name+" "+selectedUser.last_name;
        }

        public bool IsSelected
        {
            get
            {
                return selectedUser != null;
            }
        }

        internal string GetSelectedUserId()
        {
            if (selectedUser != null)
                return selectedUser.id;
            else
                return "";
        }

        internal int SelectUserById(string selUserId)
        {
            var user = users.Find(x => x.id == selUserId);
            int index = users.IndexOf(user);
            SelectUser(index);
            return index;
        }

        internal int GetSelectedUserIndex()
        {
            return users.IndexOf(selectedUser);
        }

        internal int GetCount()
        {
            return users.Count;
        }

        internal int GetOnlineCount()
        {
            return users.Count(x => x.online == "1");
        }
    }
}

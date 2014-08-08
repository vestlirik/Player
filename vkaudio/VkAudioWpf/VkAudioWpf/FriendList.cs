using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml;

namespace VkAudioWpf
{
    public delegate void Authentification();
    class FriendList:IDisposable
    {
        protected List<User> users;
        protected List<User> gettedUsers;
        private User selectedUser;
        private string userId;
        private string access_token;
        private Timer timer;
        private Authentification auth;


        public delegate void MethodContainer();

        //Событие OnCount c типом делегата MethodContainer.
        public event MethodContainer OnUsersUpdated;

        public FriendList(string _userId, string _access_token, Authentification _auth)
        {
            users = new List<User>();
            gettedUsers = new List<User>();
            selectedUser = null;
            userId = _userId;
            access_token = _access_token;
            auth = _auth;

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
            gettedUsers.Clear();

            int length = receivedUsers.ChildNodes[1].ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var user = new User(receivedUsers.ChildNodes[1].ChildNodes[i]);
                gettedUsers.Add(user);
            }

            users = gettedUsers.OrderByDescending(us => int.Parse(us.online)).ToList();
            users = users.OrderByDescending(us => us.status_audio_id != "").ToList();
        }

        public void UpdateUsers()
        {
            bool b = false;
            again:try
            {
                Uri uri = new Uri("https://api.vk.com/method/friends.get.xml?user_id=" + userId + "&order=hints&fields=photo_50, online, last_seen, status, counters, can_write_private_message, can_see_audio" + "&access_token=" + access_token + "&v=5.21");

                var x = new XmlDocument();
                x.Load(uri.ToString());
                var receivedUsers = x.GetElementsByTagName("response")[0];

                int length = receivedUsers.ChildNodes[1].ChildNodes.Count;
                for (int i = 0; i < length; i++)
                {
                    var user = new User(receivedUsers.ChildNodes[1].ChildNodes[i]);
                    var tmpUser = users.FirstOrDefault(z => z.id == user.id);
                    if (tmpUser != null)
                        tmpUser.UpdateDataFrom(user);
                    else
                        users.Add(tmpUser);
                }

                users = users.OrderBy(d => gettedUsers.IndexOf(d)).ToList();
                try
                {
                    users = users.OrderByDescending(us => us.online == "1").ToList();
                }
                catch { }
                users = users.OrderByDescending(us => us.status_audio_id != "").ToList();
            }
            catch
            {
                if (!b)
                {
                    auth();
                    b = true;
                    goto again;
                }
                else
                    System.Windows.Forms.Application.Restart();
            }
        }


        public User[] GetUsers()
        {
            return users.ToArray();
        }
        public User[] GetGettedUsers()
        {
            return gettedUsers.ToArray();
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
            if (selectedUser != null)
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
            if (selectedUser == null)
                return null;
            else
                if (selectedUser.GetPlaylist() == null)
                    return null;
                else
                    return selectedUser.GetPlaylist();
        }


        internal string GetCurrentUserName()
        {
            return selectedUser.first_name + " " + selectedUser.last_name;
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

        public bool IsCurrentUserFillTracks
        {
            get
            {
                return selectedUser.IsFilledTracks;

            }
        }

        internal void CurrentUserFilledTracks()
        {
            this.selectedUser.FillTracks();
        }

        public async void SendTrackToFriends(AudioVK audio, string[] userIds)
        {
            await Task.Factory.StartNew(() =>
            {
                if (userIds.Length == 0)
                    return;
                var attachment = "audio" + audio.owner_id + "_" + audio.aid;
                string userTo = String.Join(",", userIds);
                Uri uri = new Uri("https://api.vk.com/method/messages.send.xml?" + "user_ids=" + userTo + "&attachment=" + attachment + "&access_token=" + access_token + "&v=5.21");

                var x = new XmlDocument();
                x.Load(uri.ToString());
                if (x.ChildNodes[1].Name == "error")
                    if (x.ChildNodes[1].ChildNodes[0].InnerText == "9")
                        System.Windows.MessageBox.Show("Ви дуже часто відправляєте трек.");
                    else
                        System.Windows.MessageBox.Show("Помилка при відправці.");

            });
        }

        internal string SelectUserIdByName(string p)
        {
            User selected = gettedUsers.FirstOrDefault(x => x.first_name.Trim() + " " + x.last_name.Trim() == p);
            if (selected != null)
                return selected.id;
            else
                throw new Exception("not find user:" + p);
        }

        public void Dispose()
        {
            this.timer.Dispose();
        }
    }
}

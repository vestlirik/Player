using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VkAudioWpf
{
    class Albums
    {
        protected List<AlbumVK> albums;
        protected AlbumVK selectedAlbum;

        public Albums()
        {
            albums = new List<AlbumVK>();
        }

        public int Count()
        {
            return albums.Count;
        }

        public void DownloadAlbums(string[] data)
        {

            var userId = data[0];
            var token = data[1];
            Uri uri = new Uri("https://api.vk.com/method/audio.getAlbums.xml?owner_id=" + userId + "&access_token=" + token + "&v=5.9");

            var x = new XmlDocument();
            x.Load(uri.ToString());
            var receivedAlbums = x.GetElementsByTagName("response")[0];

            albums.Clear();

            int length = receivedAlbums.ChildNodes[1].ChildNodes.Count;

            for (int i = 0; i < length; i++)
            {
                var album = new AlbumVK(receivedAlbums.ChildNodes[1].ChildNodes[i]);
                albums.Add(album);
            }
        }

        public string[] GetAlbums()
        {
            return albums.Select(x => x.Title).ToArray();
        }


        public void SelectAlbum(int index, string userId, string token)
        {
            selectedAlbum = albums[index];
            selectedAlbum.LoadAlbum(new[] { userId, token, selectedAlbum.Id });
        }

        public AlbumVK GetSelectedAlbum()
        {
            return selectedAlbum;
        }

        public KeyValuePair<string, string>[] GetSelectedTracksForAlbum()
        {
            return albums.Select(x => new KeyValuePair<string, string>(x.Id, x.playlist == null ? "" : x.playlist.GetCurrentTrackVK() != null ? x.playlist.GetCurrentTrackVK().aid : "")).ToArray();
        }

        public void SetSelectedTracksForAlbum(KeyValuePair<string, string>[] selected)
        {
            foreach (var elm in selected)
            {
                foreach (var alb in albums)
                {
                    if (alb.Id == elm.Key)
                    {
                        if (alb.playlist != null)
                            alb.playlist.SelectTrackByAid(elm.Value);
                        break;
                    }
                }
            }
        }


        internal int GetIndexById(string p)
        {
            return albums.FindIndex(x => x.Id == p);
        }

        internal string GetIdByName(string selected)
        {
            return albums.First(x => x.Title == selected).Id;
        }
    }
}

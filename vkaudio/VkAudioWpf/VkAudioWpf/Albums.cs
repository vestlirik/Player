using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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

        public void Add(string name,string token)
        {
            Uri uri = new Uri("https://api.vk.com/method/audio.addAlbum.xml?title=" + name + "&access_token=" + token + "&v=5.9");

            var x = XDocument.Load(uri.ToString());
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
            if (index >= 0 && albums.Count>0 && albums.Count>=index+1)
            {
                selectedAlbum = albums[index];
                selectedAlbum.LoadAlbum(new[] { userId, token, selectedAlbum.Id });
            }
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

        internal void DeleteCurrent(string token)
        {
            var selAlbum = this.selectedAlbum;

            Uri uri = new Uri("https://api.vk.com/method/audio.deleteAlbum.xml?album_id=" + selAlbum.Id + "&access_token=" + token + "&v=5.9");

            var x = XDocument.Load(uri.ToString());

            if (x.Element("response").Value != "1")
                throw new Exception("Не вдлося видалити альбом");

            albums.Remove(selAlbum);
            selectedAlbum = null;



        }

        internal void Edit(string newName,string token)
        {
            var selAlbum = this.selectedAlbum;

            Uri uri = new Uri("https://api.vk.com/method/audio.editAlbum.xml?album_id=" + selAlbum.Id + "&title="+newName + "&access_token=" + token + "&v=5.9");

            var x = XDocument.Load(uri.ToString());

            if (x.Element("response").Value != "1")
                throw new Exception("Не вдалося перейменувати альбом");
        }

        internal string[] GetAlbumsIds()
        {
            return albums.Select(x => x.Id).ToArray();
        }
    }
}

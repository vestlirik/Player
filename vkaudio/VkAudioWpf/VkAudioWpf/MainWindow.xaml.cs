using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Taskbar;
using Formms = System.Windows.Forms;
using vkAudio;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Net;
using System.IO;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

namespace VkAudioWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SuppressUnmanagedCodeSecurityAttribute]
    public sealed partial class MainWindow : Window,IDisposable
    {

        //форма авторизации
        Auth auth;
        Settings sett;

        Player player;
        PlayListVk playlist;
        PlayListVk playlistAll;

        //время последнего нажатия горячей кнопки
        DateTime lastInput;

        //очередь для случайного воспроизведения
        List<int> cherga = new List<int>();

        //Кнопки для панели задач
        private ThumbnailToolBarButton buttonPlayPause;
        private ThumbnailToolBarButton buttonNext;
        private ThumbnailToolBarButton buttonPrevious;
        TaskbarManager tbManager = TaskbarManager.Instance;

        Formms.Timer timer;
        Formms.Timer timerScrobble;


        private Albums albums;
        private FriendList users;
        private Search search;
        private Reccomendations recommendations;

        private Thread globalKeyThread;



        public MainWindow()
        {
            InitializeComponent();

            timerScrobble = new System.Windows.Forms.Timer();
            timerScrobble.Interval = 1000;
            timerScrobble.Tick += timerScrobble_Tick;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 800;
            timer.Tick += timer_Tick;

            player = new Player();
            this.Focus();
            lastInput = DateTime.Now;

            timer.Stop();
            timerScrobble.Stop();

            startTime.Content = startTime.Content = "0:00";
            progressBar.Value = 0;

            //Attach the event handler 
            player.StatusChanged += new Player.OnStatusUpdate(engine_StatusChanged);

            #region taskbar
            buttonPlayPause = new ThumbnailToolBarButton
                (Properties.Resources.Hopstarter_Button_Button_Play, "Play");
            buttonPlayPause.Enabled = true;
            buttonPlayPause.Click +=
                new EventHandler<ThumbnailButtonClickedEventArgs>(playPauseButton_Click1);

            buttonNext = new ThumbnailToolBarButton
            (Properties.Resources.Hopstarter_Button_Button_Fast_Forward, "Next");
            buttonNext.Enabled = true;
            buttonNext.Click +=
                 new EventHandler<ThumbnailButtonClickedEventArgs>(nextButton_Click1);

            buttonPrevious = new ThumbnailToolBarButton(Properties.Resources.Custom_Icon_Design_Pretty_Office_8_Fast_backward, "Previous");
            buttonPrevious.Click +=
              new EventHandler<ThumbnailButtonClickedEventArgs>(prevButton_Click1);

            #endregion


            //notifyIcon1.Visible = true;
            //notifyIcon1.Icon = SystemIcons.Hand;

            logInButton.Visibility = System.Windows.Visibility.Visible;
            logOutButton.Visibility = System.Windows.Visibility.Collapsed;
            usernameLabel.Visibility = System.Windows.Visibility.Collapsed;
            updateButton.Visibility = System.Windows.Visibility.Collapsed;
            offlineButton.Visibility = System.Windows.Visibility.Collapsed;

            sett = new Settings();
            ReadSettings();

            logInButton_Click(new object(), new RoutedEventArgs());
            if (sett.LastSessionKey != "")
                logInLastButton_Click(new object(), new RoutedEventArgs());

            if (sett.VolumeLevel != null)
            {
                volumeBar.Value = sett.VolumeLevel * 100;
                player.ChangeVolume(sett.VolumeLevel);
            }
            checkBoxRepeat.IsChecked = sett.EnableRepeating;
            checkBoxShuffle.IsChecked = sett.EnableShuffling;
            playlistButton.IsChecked = sett.EnableViewPlaylist;
            BroadcastToVKButton.IsChecked = sett.EnableVKBroadcast;

            globalKeyThread=null;
        }

        void timerScrobble_Tick(object sender, EventArgs e)
        {
            listedTime++;
        }

        private void ReadSettings()
        {
            try
            {
                using (FileStream fileStream = new FileStream("sett.bin", FileMode.Open))
                {
                    IFormatter bf = new BinaryFormatter();
                    sett = (Settings)bf.Deserialize(fileStream);
                }
            }
            catch
            {
                sett.UserId = "";
                sett.VKToken = "";
            }
        }
        private void SaveSettings()
        {
            using (FileStream fileStream = new FileStream("sett.bin", FileMode.Create))
            {
                IFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, sett);
            }
        }

        private void prevButton_Click1(object sender, ThumbnailButtonClickedEventArgs e)
        {
            prevButton_Click(sender, new RoutedEventArgs());
        }

        private void nextButton_Click1(object sender, ThumbnailButtonClickedEventArgs e)
        {
            nextButton_Click(sender, new RoutedEventArgs());
        }

        private void playPauseButton_Click1(object sender, ThumbnailButtonClickedEventArgs e)
        {
            playPauseButton_Click(sender, new RoutedEventArgs());
        }

        private void MinimizeClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }


        private void Exit(object sender, RoutedEventArgs e)
        {
            if (auth != null)
                auth.Close();
            SaveSettings();
            Dispose();
            Close();

        }


        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Регистрация горячих клавиш
            Formms.MethodInvoker mi = new Formms.MethodInvoker(WaitKey);
            mi.BeginInvoke(null, null);
            #endregion

            Window window = Window.GetWindow(this);
            var wih = new WindowInteropHelper(window);
            IntPtr hWnd = wih.Handle;

            if (TaskbarManager.IsPlatformSupported)
                TaskbarManager.Instance.ThumbnailToolBars.AddButtons
                   (hWnd, buttonPrevious, buttonPlayPause, buttonNext);
        }

        private void GetAuth()
        {
            if (sett.VKToken == "")
            {
                ////Авторизация в вк

                auth = new Auth();
                auth.ShowDialog();
                if (!auth.IsAuth)
                {
                    auth.Close();
                    Application.Current.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Send);
                    this.Close();
                }
                sett.VKToken = auth.Token;
                sett.UserId = auth.UserId;
            }

            try
            {
                ////загрузка данных о профиле
                XmlDocument x = new XmlDocument();
                x.Load("https://api.vk.com/method/getProfiles.xml?uid=" + sett.UserId + "&access_token=" + sett.VKToken);
                // Парсим
                var el = x.GetElementsByTagName("user")[0];
                usernameLabel.Content = el.ChildNodes[1].InnerText + " " + el.ChildNodes[2].InnerText;
                logInButton.Visibility = System.Windows.Visibility.Collapsed;
                usernameLabel.Visibility = System.Windows.Visibility.Visible;
                updateButton.Visibility = System.Windows.Visibility.Visible;
                offlineButton.Visibility = System.Windows.Visibility.Visible;
                logOutButton.Visibility = System.Windows.Visibility.Visible;
            }
            catch
            {
                try
                {
                    auth = new Auth();
                    auth.ShowDialog();
                    if (!auth.IsAuth)
                    {
                        auth.Close();
                        Application.Current.Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Send);
                        this.Close();
                    }
                    sett.VKToken = auth.Token;
                    sett.UserId = auth.UserId;

                    ////загрузка данных о профиле
                    XmlDocument x = new XmlDocument();
                    x.Load("https://api.vk.com/method/getProfiles.xml?uid=" + sett.UserId + "&access_token=" + sett.VKToken);
                    // Парсим
                    var el = x.GetElementsByTagName("user")[0];
                    usernameLabel.Content = el.ChildNodes[1].InnerText + " " + el.ChildNodes[2].InnerText;
                    logInButton.Visibility = System.Windows.Visibility.Collapsed;
                    usernameLabel.Visibility = System.Windows.Visibility.Visible;
                    updateButton.Visibility = System.Windows.Visibility.Visible;
                    offlineButton.Visibility = System.Windows.Visibility.Visible;
                    logOutButton.Visibility = System.Windows.Visibility.Visible;
                }
                catch
                {
                    MessageBox.Show("Неможливо з'єднатися з vk.com");
                }
            }


        }

        private async void logInButton_Click(object sender, RoutedEventArgs e)
        {
            GetAuth();
            if (sett.VKToken != "")
            {
                //albums
                albums = new Albums();
                await Task.Factory.StartNew(() => albums.DownloadAlbums(new string[] { sett.UserId, sett.VKToken }));
                FillAlbums(albums);

                //my tracks
                playlist = new PlayListVk();
                playlistAll = playlist;
                await Task.Factory.StartNew(() => playlist.DownloadTracks(new string[] { sett.UserId, sett.VKToken }));

                tracksCountLabel.Content = playlist.Count() + " треків";
                FillListBox((PlayListVk)playlist, listBox, false, true, true);


                //users
                loadFriendList();

                //search
                search = new Search(sett.VKToken);
                //recommeds
                recommendations = new Reccomendations(sett.VKToken);
                UpdateRecommendations();
            }
        }

        private void FillAlbums(Albums albums)
        {
            albumsCombobox.Items.Clear();
            foreach (var alb in albums.GetAlbums())
            {
                albumsCombobox.Items.Add(alb);
            }
        }

        private void FillListBox(PlayListVk pls, ListBox listbx, bool allowAdding, bool allowDeleting, bool isMyAudioTab)
        {
            listbx.Items.Clear();
            foreach (var elm in pls.GetTrackListVK())
            {
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Height = 30;
                lstItem.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                lstItem.Margin = new Thickness(0, 0, 0, 3);
                lstItem.Style = this.FindResource("lstStyle") as Style;

                #region creating grid for listboxItem

                bool isAllowAddCurrTrack = allowAdding;
                bool isAllowDeletingCurrTrack = allowDeleting;
                if (allowAdding)
                    isAllowAddCurrTrack = !elm.IsAdded;

                int sumSize = 0;
                sumSize += 40;
                if (isAllowAddCurrTrack)
                    sumSize += 30;
                sumSize += 70;
                if (isAllowDeletingCurrTrack)
                    sumSize += 80;
                if (isMyAudioTab)
                    sumSize += 80;


                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(listbx.Width - sumSize - 30) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });


                if (isAllowDeletingCurrTrack)
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });

                if (isAllowAddCurrTrack)
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30) });

                if (isMyAudioTab)
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

                if (isAllowDeletingCurrTrack)
                    grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });
                #endregion


                int colemn = 0;

                #region add elements in grid for listboxitem
                System.Windows.Controls.Label lbl = new System.Windows.Controls.Label();
                lbl.Content = elm.artist + " - " + elm.title;
                Grid.SetColumn(lbl, colemn);
                colemn++;
                grid.Children.Add(lbl);

                lbl = new System.Windows.Controls.Label();
                lbl.Content = elm.DurationString;
                Grid.SetColumn(lbl, colemn);
                colemn++;
                grid.Children.Add(lbl);

                Button btn;


                if (isAllowDeletingCurrTrack)
                {
                    Button editBtn = new Button();
                    editBtn.Style = this.FindResource("roundedButton") as Style;

                    Image img3 = new Image();
                    img3.Height = 20;
                    img3.Width = 40;
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("/images/Edit-icon.png", UriKind.Relative);
                    bitmap.EndInit();
                    img3.Source = bitmap;

                    editBtn.Content = img3;

                    editBtn.Click += (object send, RoutedEventArgs ee) =>
                    {
                        editedTrack = elm;
                        trackNameTextBox.Text = elm.title;
                        artistNameTextBox.Text = elm.artist;
                        //popupTrack.Visibility = System.Windows.Visibility.Visible;
                        popupTrack.IsOpen = true;

                    };

                    Grid.SetColumn(editBtn, colemn);
                    colemn++;
                    grid.Children.Add(editBtn);
                }

                #region add button
                if (isAllowAddCurrTrack)
                {
                    Button addBtn = new Button();
                    addBtn.Style = this.FindResource("roundedButton") as Style;
                    addBtn.Content = "+";

                    addBtn.Click += (object send, RoutedEventArgs ee) =>
                    {
                        Button currBtn = ((Button)send);
                        currBtn.IsEnabled = false;
                        if (!elm.Add(sett.VKToken))
                        {
                            MessageBox.Show("Виникла помилка з додаваням треку");
                            currBtn.IsEnabled = true;
                        }
                        ShowNotiff("Аудіозапис додано");
                    };

                    Grid.SetColumn(addBtn, colemn);
                    colemn++;
                    grid.Children.Add(addBtn);
                }
                #endregion

                #region album combobox
                if (isMyAudioTab)
                {
                    ComboBox cbx = new ComboBox();
                    cbx.Foreground = Brushes.White;
                    cbx.Cursor = Cursors.Hand;
                    cbx.IsEditable = false;
                    cbx.IsTextSearchEnabled = false;
                    cbx.IsTextSearchCaseSensitive = false;
                    cbx.StaysOpenOnEdit = false;
                    //cbx.IsReadOnly = true;
                    var albms = albums.GetAlbums();
                    cbx.Items.Add("Без альбому");
                    cbx.ToolTip = "Без пльбому";
                    foreach (var alb in albms)
                        cbx.Items.Add(alb);
                    cbx.SelectedIndex = 0;
                    if (elm.album_id.Trim() != "")
                    {
                        cbx.SelectedIndex = albums.GetIndexById(elm.album_id) + 1;
                    }

                    cbx.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
                    {
                        string selected = cbx.SelectedItem.ToString();
                        ((ComboBox)sender).ToolTip = selected;
                        if (selected != "Без альбому")
                        {
                            elm.ChangeAlbum(id: albums.GetIdByName(selected), token: sett.VKToken);
                        }
                        else
                            elm.ChangeAlbum(id: "", token: sett.VKToken);
                    };


                    Grid.SetColumn(cbx, colemn);
                    colemn++;
                    grid.Children.Add(cbx);

                }
                #endregion

                btn = new Button();
                btn.Style = this.FindResource("roundedButton") as Style;
                btn.Content = "Скачати";

                btn.Click += (object send, RoutedEventArgs ee) =>
                {
                    ((Button)send).IsEnabled = false;
                    DownloadFile(elm, (Button)send);
                };

                Grid.SetColumn(btn, colemn);
                colemn++;
                grid.Children.Add(btn);



                #region add deleting button
                if (isAllowDeletingCurrTrack)
                {
                    btn = new Button();
                    btn.Style = this.FindResource("roundedButton") as Style;
                    btn.Content = "X";

                    btn.Click += (object send, RoutedEventArgs ee) =>
                    {
                        DeleteTrack(listBox.Items.IndexOf(lstItem));
                    };

                    Grid.SetColumn(btn, colemn);
                    colemn++;
                    grid.Children.Add(btn);
                }
                #endregion
                #endregion

                lstItem.Content = grid;
                var cntMenu = new ContextMenu();
                MenuItem mi;
                if (elm.HasLyrics)
                {
                    mi = new MenuItem();
                    mi.Header = "Показати текст";
                    mi.Click += new RoutedEventHandler((sender, e) =>
                    {
                        Thread t = new Thread(() => MessageBox.Show(elm.GetLyrics(sett.VKToken), elm.Name + " lirycs"));
                        t.Start();
                    });
                    cntMenu.Items.Add(mi);
                }

                mi = new MenuItem();
                mi.Header = "Знайти інші треки " + elm.artist.Substring(0, elm.artist.Length > 20 ? 20 : elm.artist.Length);
                mi.Click += new RoutedEventHandler((sender, e) =>
                {
                    searchQueryTextBox.Text = elm.artist;
                    searchTab.Focus();
                    SearchQueryButton_Click(null, null);
                });
                cntMenu.Items.Add(mi);

                mi = new MenuItem();
                mi.Header = "Рекомендовані треки " + elm.artist.Substring(0, elm.artist.Length > 20 ? 20 : elm.artist.Length);
                mi.Click += new RoutedEventHandler(async (sender, e) =>
                {
                    reccomedTab.Focus();
                    listRecommedTracksBox.SelectedIndex = -1;
                    listRecommedTracksBox.SelectedIndex = 0;
                    listRecommedTracksBox.SelectedIndex = -1;
                    await Task.Factory.StartNew(() => recommendations.LoadReccomendationsByAudio(elm));
                    if (recommendations.Playlist.Count() == 0)
                    {
                        MessageBox.Show("Немає рекомендації до цього треку.");
                        recommendations.LoadReccomendations();
                    }
                    else
                    {
                        recommedCountLabel.Content = recommendations.GetSearchedCount() + " треків за рекомендацію \"" + recommendations.Query + "\"";
                        FillListBox(recommendations.Playlist, listRecommedTracksBox, true, false, false);
                    }
                });

                cntMenu.Items.Add(mi);
                lstItem.ContextMenu = cntMenu;

                if (listbx == listBox)
                    lstItem.MouseDoubleClick += lstItem_MouseDoubleClick;
                if (listbx == listAlbumBox)
                    lstItem.MouseDoubleClick += lstItemAlbum_MouseDoubleClick;
                if (listbx == listUserTracksBox)
                    lstItem.MouseDoubleClick += lstItemUser_MouseDoubleClick;
                if (listbx == listSearchedTracksBox)
                    lstItem.MouseDoubleClick += lstItemSearch_MouseDoubleClick;
                if (listbx == listRecommedTracksBox)
                    lstItem.MouseDoubleClick += lstItemRecommed_MouseDoubleClick;
                //lstItem.MouseEnter += lstItem_MouseEnter;

                listbx.Items.Add(lstItem);

            }
        }

        private void FillUsers(ListBox listbx)
        {
            listbx.Items.Clear();
            foreach (var elm in users.GetUsers())
            {
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Height = 50;
                lstItem.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                lstItem.Margin = new Thickness(0, 0, 0, 3);
                lstItem.Style = this.FindResource("lstStyle") as Style;

                #region creating grid for listboxItem
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(50) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(180) });
                #endregion

                var image = elm.GetImage;

                if (image.Parent != null)
                {
                    var parent = (Grid)image.Parent;
                    parent.Children.Remove(image);
                }
                Grid.SetColumn(image, 0);
                grid.Children.Add(image);


                StackPanel stackPnl = new StackPanel();
                stackPnl.Orientation = Orientation.Vertical;

                StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal };

                Label lbl = new Label() { Height = 25, FontSize = 10, Margin = new Thickness(0, 0, 0, 0) };
                lbl.Content = elm.first_name + " " + elm.last_name;

                stack.Children.Add(lbl);

                if (elm.can_see_audio == "0")
                {
                    Image img2 = new Image();
                    img2.Height = 15;
                    img2.Width = 15;
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("/images/lock-it-07-icon.png", UriKind.Relative);
                    bitmap.EndInit();
                    img2.Source = bitmap;

                    img2.ToolTip = "Заборонений перегляд аудіозаписів";

                    img2.Margin = new Thickness(0, -5, 0, 0);


                    stack.Children.Add(img2);
                }

                stackPnl.Children.Add(stack);


                stack = new StackPanel() { Orientation = Orientation.Horizontal };
                if (elm.last_seen_time.Trim().Length > 0)
                {
                    lbl = new Label() { Height = 30, FontSize = 10, Margin = new Thickness(0, -15, 0, 0) };

                    lbl.Content = elm.online == "1" ? "online" : (elm.sex == "1" ? "була" : "був") + " " + Settings.UnixTimeStampToDateTime(double.Parse(elm.last_seen_time));
                    stack.Children.Add(lbl);
                }

                if (elm.last_seen_platform.Trim().Length > 0)
                {

                    Image img3 = new Image();
                    img3.Height = 15;
                    img3.Width = 15;
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri("/images/devices/" + elm.last_seen_platform_string + ".png", UriKind.Relative);
                    bitmap.EndInit();
                    img3.Source = bitmap;
                    img3.ToolTip = elm.last_seen_platform_string[0].ToString().ToUpper() + elm.last_seen_platform_string.Substring(1);

                    img3.Margin = new Thickness(0, -15, 0, 0);

                    stack.Children.Add(img3);
                }

                stackPnl.Children.Add(stack);

                lbl = new Label() { Height = 25, FontSize = 10, Margin = new Thickness(0, -15, 0, 0) };
                lbl.Content = elm.status;
                if (elm.status.Length > 30)
                    lbl.ToolTip = elm.status;
                else
                    lbl.ToolTip = null;
                stackPnl.Children.Add(lbl);

                Grid.SetColumn(stackPnl, 1);
                grid.Children.Add(stackPnl);

                lstItem.Content = grid;

                listbx.Items.Add(lstItem);

            }
            usersInfoLabel.Content = users.GetCount() + " друзів  " + users.GetOnlineCount() + " онлайн";
        }



        void lstItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist = playlistAll;
            playlist.SelTrack = listBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked == true)
                if (cherga.Count > 0 && cherga[cherga.Count - 1] != playlist.SelTrack)
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        void lstItemAlbum_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist = albums.GetSelectedAlbum().playlist;
            playlist.SelTrack = listAlbumBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked == true)
                if (cherga.Count > 0 && cherga[cherga.Count - 1] != playlist.SelTrack)
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }
        void lstItemUser_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist = users.GetSelectedUserPlaylist();
            playlist.SelTrack = listUserTracksBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked == true)
                if (cherga.Count > 0 && cherga[cherga.Count - 1] != playlist.SelTrack)
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }
        void lstItemSearch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist = search.Playlist;
            playlist.SelTrack = listSearchedTracksBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked == true)
                if (cherga.Count > 0 && cherga[cherga.Count - 1] != playlist.SelTrack)
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }
        void lstItemRecommed_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist = recommendations.Playlist;
            playlist.SelTrack = listRecommedTracksBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked == true)
                if (cherga.Count > 0 && cherga[cherga.Count - 1] != playlist.SelTrack)
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }



        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            PrevTrack();
        }

        private void playPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseButton();
        }

        int currInCherga = -1;

        private void PrevTrack()
        {
            if (playlist != null && playlist.Count() > 0)
            {
                if (checkBoxShuffle.IsChecked == true)
                {
                    currInCherga = cherga.IndexOf(playlist.SelTrack);
                    if (currInCherga > 0)
                    {
                        currInCherga--;
                        playlist.SelTrack = cherga[currInCherga];
                    }
                    else
                        return;
                }
                else
                {
                    if (playlist.SelTrack == 0)
                        playlist.SelTrack = playlist.Count() - 1;
                    else
                        playlist.SelTrack--;

                }
                PlayTrack();
            }
        }

        private void NextTrack()
        {
            if (playlist != null && playlist.Count() > 0)
            {
                if (checkBoxShuffle.IsChecked == true)
                {
                    if (currInCherga < cherga.Count - 1 && cherga.Count > 0 && currInCherga != -1)
                    {
                        currInCherga++;
                        playlist.SelTrack = cherga[currInCherga];
                    }
                    else
                    {
                        playlist.SelTrack = (new Random()).Next(playlist.Count() - 1);
                        cherga.Add(playlist.SelTrack);
                        currInCherga = cherga.Count - 1;
                    }
                }
                else
                {
                    if (playlist.SelTrack == playlist.Count() - 1)
                        playlist.SelTrack = 0;
                    else
                        playlist.SelTrack++;
                }




                PlayTrack();
            }
        }

        private void PlayPauseButton()
        {
            var z = player.GetPlayerstatus();
            if (player.GetPlayerstatus() == PLAYER_STATUS.PLAYER_STATUS_PAUSED)
            {
                player.Resume();

            }
            else if (player.GetPlayerstatus() == PLAYER_STATUS.PLAYER_STATUS_PLAYING)
            {
                player.Pause();
            }
            else
            {
                if (playlist != null && playlist.Count() > 0)
                {
                    if (playlist.SelTrack > -1)
                        PlayTrack();
                    else
                    {
                        playlist.SelTrack = 0;
                        PlayTrack();
                    }
                }
                else
                {
                    MessageBox.Show("Не вибрано!!");
                }
            }

        }

        private void PlayTrack()
        {
            prevValue = -1;
            Audio currSong = null;
            var plstType = playlist.GetType();
            currSong = ((PlayListVk)playlist).GetCurrentTrackVK();
            player.AttachTrack((AudioVK)currSong);
            //set status
            var audioId = ((AudioVK)currSong).owner_id + "_" + ((AudioVK)currSong).aid;
            if (sett.EnableVKBroadcast)
                ((PlayListVk)playlist).SetStatus(audioId, sett.VKToken);
            //scrobble
            if (sett.LastSessionKey != "")
            {
                try
                {
                    LastUpdateNowTrack(((AudioVK)currSong).title, ((AudioVK)currSong).artist);
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine("not write now track to last fm:" + ex.InnerException.Message);
                }
                scrobbleTime = (int)(double.Parse(((AudioVK)currSong).duration) / 2);
                listedTime = 0;
                IsScrobled = false;

            }



            if (playlist == playlistAll)
                listBox.SelectedIndex = playlist.SelTrack;
            else
                if (albums.GetSelectedAlbum() != null && playlist == albums.GetSelectedAlbum().playlist)
                    listAlbumBox.SelectedIndex = playlist.SelTrack;
                else
                    if (playlist == users.GetSelectedUserPlaylist())
                        listUserTracksBox.SelectedIndex = playlist.SelTrack;
                    else
                        if (playlist == search.Playlist)
                            listSearchedTracksBox.SelectedIndex = playlist.SelTrack;
                        else
                            if (playlist == recommendations.Playlist)
                                listRecommedTracksBox.SelectedIndex = playlist.SelTrack;

            player.Play();


            titleLabel.Content = currSong.Name;

            progressBar.Minimum = 0;
                progressBar.Maximum = int.Parse(((AudioVK)currSong).duration);
                endTime.Content = ((AudioVK)currSong).DurationString;

            timer.Enabled = true;

            this.headerLabel.Content = titleLabel.Content + " - Zeus";
            this.Title = titleLabel.Content + " - Zeus";

            var tmpStr = titleLabel.Content.ToString();
            var str = tmpStr.Length > 64 ? tmpStr.Substring(0, 64) : tmpStr;
            //notifyIcon1.ShowBalloonTip(500, "Наступний трек", str, ToolTipIcon.Info);
            GetAlbumArt(((AudioVK)currSong).artist, ((AudioVK)currSong).title,((AudioVK)currSong).aid);

        }

        private void GetAlbumArt(string artist, string title,string aid)
        {
            new Thread(() =>
                {
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                    {

                        pictureBox.Source = null;

                        return null;

                    }), null);

                    if (Settings.CheckImage(aid + ".jpg").Trim() != "")
                    {this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                        {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        string path = Settings.CheckImage(aid + ".jpg").Trim();
                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                        bitmap.EndInit();

                        pictureBox.Source = bitmap;
                        SetTaskbarthumbnail();

                        return null;

                        }), null);
                    }
                    else
                    try
                    {

                        string url = "http://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key=b7d62b44095bbe482030e12b4aa33572&artist=" + RemoveOtherSymbols(artist) + "&track=" + RemoveOtherSymbols(title);
                        XDocument doc = XDocument.Load(url);

                        var album = doc.Descendants("album");

                        var image = album.Elements().ToArray()[6].Value;

                        if (image != "")
                        {
                            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                        {
                            bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            if (Settings.CheckImage(aid + ".jpg").Trim() == "")
                                Settings.DownloadRemoteImageFile(image, aid + ".jpg");
                            string path = Settings.CheckImage(aid + ".jpg").Trim();
                            if (path != "")
                                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                            else
                                bitmap.UriSource = new Uri(image, UriKind.Absolute);
                            bitmap.EndInit();

                            pictureBox.Source = bitmap;
                            SetTaskbarthumbnail();

                            return null;

                        }), null);
                        }
                        return;
                    }
                    catch
                    {
                        try
                        {
                            string url = "http://ws.audioscrobbler.com/2.0/?method=artist.getinfo&api_key=b7d62b44095bbe482030e12b4aa33572&artist=" + RemoveOtherSymbols(artist);
                            XDocument doc = XDocument.Load(url);

                            var album = doc.Descendants("artist");

                            var image = album.Elements().ToArray()[6].Value;


                            if (image != "")
                            {
                                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                                {
                                    bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    if (Settings.CheckImage(aid + ".jpg").Trim() == "")
                                        Settings.DownloadRemoteImageFile(image, aid + ".jpg");
                                    string path = Settings.CheckImage(aid + ".jpg").Trim();
                                    if (path != "")
                                        bitmap.UriSource = new Uri(path, UriKind.Absolute);
                                    else
                                        bitmap.UriSource = new Uri(image, UriKind.Absolute);
                                    bitmap.EndInit();

                                    pictureBox.Source = bitmap;
                                    SetTaskbarthumbnail();

                                    return null;

                                }), null);
                            }
                            return;
                        }
                        catch
                        {
                            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                            {

                                pictureBox.Source = null;

                                return null;

                            }), null);
                        }

                    }

                }).Start();


        }
        BitmapImage bitmap;
        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            NextTrack();
        }

        #region Register global keys
        private void WaitKey()
        {
            globalKeyThread = new Thread(() =>
            {
                while (globalKeyThread.IsAlive)
                {

                    short res1 = GetAsyncKeyState(VK_MEDIA_PLAY_PAUSE);
                    short res2 = GetAsyncKeyState(VK_MEDIA_PREV_TRACK);
                    short res3 = GetAsyncKeyState(VK_MEDIA_NEXT_TRACK);
                    short res4 = GetAsyncKeyState(VK_MEDIA_STOP);

                    //время в мс между нажатиями горячих кнопок
                    int keyPause = 200;

                    if (res1 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                        App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                        {
                            PlayPauseButton();
                            lastInput = DateTime.Now;
                            return;
                        }));
                    else
                        if (res2 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                            App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                            {
                                PrevTrack();
                                lastInput = DateTime.Now;
                                return;
                            }));
                        else
                            if (res3 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                                App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                                {
                                    NextTrack();
                                    lastInput = DateTime.Now;
                                    return;
                                }));
                            else
                                if (res4 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                                    App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                                    {
                                        player.Stop();
                                        lastInput = DateTime.Now;
                                        return;
                                    }));
                    Thread.Sleep(50);
                }
            });
            globalKeyThread.Start();
                
        }

        public const int VK_VOLUME_MUTE = 0xAD;
        public const int VK_VOLUME_DOWN = 0xAE;
        public const int VK_VOLUME_UP = 0xAF;
        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;
        public const int VK_MEDIA_STOP = 0xB2;
        public const int VK_MEDIA_PLAY_PAUSE = 0xB3;

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern short GetAsyncKeyState([MarshalAs(UnmanagedType.I2)]short vkey);
        #endregion

        double prevValue = -1;
        int countPrevValue = 0;
        private void timer_Tick(object sender, EventArgs e)
        {
            //await Task.Factory.StartNew(() =>
            //    {

            //        this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
            //        {


            if (prevValue == progressBar.Value)
                countPrevValue++;
            else
                countPrevValue = 0;
                        if (progressBar.Value >= progressBar.Maximum || countPrevValue>3)
                        {
                            countPrevValue = 0;
                            if (sett.EnableRepeating)
                                PlayTrack();
                            else
                                NextTrack();
                        }
                        prevValue = progressBar.Value;
                        try
                        {
                            if (progressBar.Value <= progressBar.Maximum)
                            {
                                ChangeProggressBackGround();
                                progressBar.Value = (int)player.CurruntPosition;
                                startTime.Content = player.CurruntPositionString;
                                tbManager.SetProgressValue((int)progressBar.Value, (int)progressBar.Maximum, Process.GetCurrentProcess().MainWindowHandle);
                            }

                        }
                        catch { }
                        if (sett.LastSessionKey!="" && listedTime >= scrobbleTime && !IsScrobled)
                        {
                            var currSong = ((PlayListVk)playlist).GetCurrentTrackVK();
                            try
                            {
                                ScrobbleTrack(((AudioVK)currSong).title, ((AudioVK)currSong).artist);
                                IsScrobled = true;
                            }
                            catch { }
                            timerScrobble.Stop();
                        }
                //        return null;
                //    }), null);
                //});

        }

        private void ChangeProggressBackGround()
        {
            double percentage = player.GetDowloadedPercentage() / 100f;
            LinearGradientBrush gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 1);

            GradientStop stop = new GradientStop();
            stop.Color = Colors.DarkSlateGray;
            stop.Offset = percentage - 0.01;
            gradient.GradientStops.Add(stop);

            stop = new GradientStop();
            stop.Color = Colors.DarkSlateGray;
            stop.Offset = percentage;
            gradient.GradientStops.Add(stop);

            stop = new GradientStop();
            stop.Color = Colors.Transparent;
            stop.Offset = percentage + 0.01;
            gradient.GradientStops.Add(stop);

            stop = new GradientStop();
            stop.Color = Colors.Transparent;
            stop.Offset = 1;
            gradient.GradientStops.Add(stop);

            progressBar.Background = gradient;
        }

        private void progressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double dblValue;
            int point = (int)e.GetPosition(progressBar).X;
            dblValue = (point / (double)progressBar.Width) * progressBar.Maximum;
            player.CurruntPosition = Convert.ToInt32(dblValue);
        }

        #region Player events handled here
        void engine_StatusChanged(PLAYER_STATUS status,EventArgs e)
        {
            switch (status)
            {
                case PLAYER_STATUS.PLAYER_STATUS_ENDED:
                    OnStatusEnded();
                    break;
                case PLAYER_STATUS.PLAYER_STATUS_NOT_READY:
                    OnStatusNotReady();
                    break;
                case PLAYER_STATUS.PLAYER_STATUS_PAUSED:
                    OnStatusPaused();
                    break;
                case PLAYER_STATUS.PLAYER_STATUS_PLAYING:
                    OnStatusPlaying();
                    break;
                case PLAYER_STATUS.PLAYER_STATUS_READY_STOPPED:
                    //This event will come immediately after the attachfile so we can say the playback is about to start
                    OnStatusStopped();
                    break;
            }
        }


        private void OnStatusStopped()
        {
            this.Title = "Zeus";
            headerLabel.Content = "Zeus";
            titleLabel.Content = "Author - Title";
            timer.Stop();
            progressBar.Value = progressBar.Maximum = 0;

            if (!IsScrobled)
                timerScrobble.Stop();

            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
            }

            ChangePlayPauseIconInButton(false);
        }

        private void OnStatusEnded()
        {
            ChangePlayPauseIconInButton(false);
            if (!IsScrobled)
                timerScrobble.Stop();
            NextTrack();
        }

        private void OnStatusPaused()
        {
            ChangePlayPauseIconInButton(false);
            tbManager.SetProgressState(TaskbarProgressBarState.Paused);
            timer.Stop();
            if (!IsScrobled)
            {
                timerScrobble.Start();
            }

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
        }

        private void OnStatusPlaying()
        {
            ChangePlayPauseIconInButton(true);
            tbManager.SetProgressState(TaskbarProgressBarState.Normal);
            timer.Start();
            if (!IsScrobled && sett.LastSessionKey!="")
                timerScrobble.Start();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Pause;
        }
        bool IsScrobled = false;
        double listedTime = 0;
        double scrobbleTime = 0;
        private void OnStatusNotReady()
        {
            ChangePlayPauseIconInButton(false);
        }
        #endregion

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
        }

        private void checkBoxShuffle_Checked(object sender, RoutedEventArgs e)
        {
            cherga.Clear();
            currInCherga = -1;
            sett.EnableShuffling = ((ToggleButton)sender).IsChecked ?? true;
        }

        private void listBox_MouseMove(object sender, MouseEventArgs e)
        {
            //if (!listBox.IsFocused)
            //    listBox.Focus();
        }

        private void SetTaskbarthumbnail()
        {


            if (playlistButton.IsChecked == true)
            {
                taskInfo.ThumbnailClipMargin = new Thickness(0, 35, 650, 420);
            }
            if (playlistButton.IsChecked == false)
            {
                taskInfo.ThumbnailClipMargin = new Thickness(0, 35, 650, 0);
            }

        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key < Key.A && e.Key > Key.Z) || (e.Key < Key.D0 && e.Key > Key.D9))
            {
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Enter)
                if (e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl)
                {
                    if (playlistAll != null && playlistAll.Count() > 0)
                    {
                        var searchText = searchBox.Text.Trim();
                        if (searchText == "")
                        {
                            listBox.SelectedIndex = playlistAll.SelTrack;
                            return;
                        }
                        for (int i = 0; i < playlistAll.Count(); i++)
                        {
                            if (playlistAll.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                listBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (searchBox.Text.Trim() != "" && listBox.SelectedIndex != -1)
                {
                    DoubleListBoxClick();
                }
        }

        private void DoubleListBoxClick()
        {
            if (playlist != playlistAll)
            {
                cherga.Clear();
                playlist = playlistAll;
            }
            playlist.SelTrack = listBox.SelectedIndex;
            if (sett.EnableShuffling)
                if (cherga.Count > 0)
                {
                    if (cherga[cherga.Count - 1] != playlist.SelTrack)
                        cherga.Add(playlist.SelTrack);
                }
                else
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var searchText = searchBox.Text.Trim();
            if (playlistAll != null && playlistAll.Count() > 0)
                if (searchText != "" && listBox.SelectedIndex != -1)
                {
                    for (int i = listBox.SelectedIndex + 1; i < listBox.Items.Count; i++)
                    {
                        if (playlistAll.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listBox.SelectedIndex = i;
                            break;
                        }
                        if (i == listBox.Items.Count)
                        {
                            listBox.SelectedIndex = playlistAll.SelTrack;
                            return;
                        }
                    }
                }
            searchBox.Focus();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var searchText = searchBox.Text.Trim();
            if (playlistAll != null && playlistAll.Count() > 0)
                if (searchText != "" && listBox.SelectedIndex != -1)
                {
                    for (int i = listBox.SelectedIndex - 1; i > 0; i--)
                    {
                        if (playlistAll.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listBox.SelectedIndex = i;
                            break;
                        }
                        if (i == 0)
                        {
                            listBox.SelectedIndex = playlistAll.SelTrack;
                            return;
                        }
                    }
                }
            searchBox.Focus();
        }

        private void DeleteTrack(int index)
        {
            bool b = playlist.SelTrack == index;
            var res = Formms.MessageBox.Show("Видалення файлу", "Точно видалити файл?", Formms.MessageBoxButtons.YesNo, Formms.MessageBoxIcon.Question,
    Formms.MessageBoxDefaultButton.Button2);
            if (res == Formms.DialogResult.Yes)
            {
                ((PlayListVk)playlist).RemoveFile(index, sett.UserId, sett.VKToken);

                listBox.Items.RemoveAt(index);

                tracksCountLabel.Content = playlist.Count() + " треків";
                if (b)
                    PlayTrack();
            }

        }

        private void listBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                int selIndex = listBox.SelectedIndex;
                playlistAll.Remove(selIndex);
                if (selIndex <= playlistAll.SelTrack)
                    playlistAll.SelTrack--;
                listBox.Items.RemoveAt(selIndex);
                if (sett.EnableShuffling)
                {
                    if (cherga.Contains(selIndex))
                        cherga.Remove(selIndex);
                }
                listBox.SelectedIndex = selIndex;
            }
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                int selIndex = listBox.SelectedIndex;

                DeleteTrack(selIndex);

                if (playlistAll.SelTrack == selIndex)
                {
                    if (sett.EnableShuffling)
                    {
                        if (cherga.Contains(selIndex))
                            cherga.Remove(selIndex);
                    }
                    PlayTrack();
                }
                if (selIndex < playlistAll.SelTrack)
                    playlistAll.SelTrack--;
                listBox.SelectedIndex = selIndex;
            }
        }

        private void listBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoubleListBoxClick();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            listBox.SelectedIndex = -1;
            listBox.SelectedIndex = playlistAll.SelTrack;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            searchBox.Clear();
            listBox.SelectedIndex = playlistAll.SelTrack;
        }


        private void ChangePlayPauseIconInButton(bool play)
        {
            if (play)
            {
                Rectangle rect = new Rectangle();
                rect.Width = 5;
                rect.Height = 15;
                rect.Fill = Brushes.White;
                rect.Margin = new Thickness(-2, 0, 0, 0);

                Rectangle rect1 = new Rectangle();
                rect1.Margin = new Thickness(8, 0, 0, 0);
                rect1.Width = 5;
                rect1.Height = 15;
                rect1.Fill = Brushes.White;

                platPauseCanvas.Children.Clear();
                platPauseCanvas.Children.Add(rect);
                platPauseCanvas.Children.Add(rect1);
            }
            else
            {
                Polygon triangle = new Polygon();
                triangle.Points.Add(new Point(0, 0));
                triangle.Points.Add(new Point(16, 10));
                triangle.Points.Add(new Point(0, 20));
                triangle.Stroke = Brushes.Black;
                triangle.Fill = Brushes.White;
                triangle.Margin = new Thickness(0, -2, 0, 0);

                platPauseCanvas.Children.Clear();
                platPauseCanvas.Children.Add(triangle);

            }
        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }


        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            sett.EnableVKBroadcast = ((ToggleButton)sender).IsChecked ?? true;
        }

        private double GetSize(string url)
        {

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "HEAD";
            HttpWebResponse resp = (HttpWebResponse)(req.GetResponse());
            double ss = ((double)resp.ContentLength) / 1000000;
            req.Abort();
            return ss;
        }

        private void DownloadFile(AudioVK track, Button button)
        {
            string filepath = Directory.GetCurrentDirectory() + "\\" + player.FOLDER + "\\" + track.aid + ".mp3";
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Downloads\\Zeus\\";
            string name = track.artist + " - " + track.title;
            if (File.Exists(filepath))
                if (player.GetDuration(filepath) == track.duration)
                {
                    File.Copy(filepath, path + name + ".mp3", true);
                    MessageBox.Show(name + " завантажена до папки Downloads/Zeus");
                    button.IsEnabled = true;
                    return;
                }

            new Thread(() =>
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    WebClient wb = new WebClient();
                    name.Replace("\"", "");
                    wb.DownloadFile(new Uri(track.GetLocation), path + name + ".mp3");
                    wb.Dispose();
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                    {
                        MessageBox.Show(name + " завантажена до папки Downloads/Zeus");
                        button.IsEnabled = true;
                        return null;
                    }), null);
                }).Start();
        }

        private void logOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (auth == null)
            {
                auth = new Auth();
                auth.LogOut();
            }
            else
                auth.LogOut();
            sett.VKToken = "";
            sett.UserId = "";

            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void ToggleButton_Click_1(object sender, RoutedEventArgs e)
        {
            var currTrack = ((PlayListVk)playlist).GetCurrentTrackVK();
            string selTrack = "";
            if (currTrack != null)
                selTrack = currTrack.aid;
            if (reverseButton.IsChecked == true)
            {
                ((PlayListVk)playlist).ReverseOn();
            }
            else
                ((PlayListVk)playlist).ReverseOff();
            FillListBox((PlayListVk)playlist, listBox, false, true, true);
            if (currTrack != null)
            {
                ((PlayListVk)playlist).SelectTrackByAid(selTrack);
                listBox.SelectedIndex = ((PlayListVk)playlist).SelTrack;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlist != null)
            {
                var currTrack = ((PlayListVk)playlist).GetCurrentTrackVK();
                string selTrack = "";
                if (currTrack != null)
                    selTrack = currTrack.aid;

                if (sortCombobox.SelectedIndex == 0)
                    ((PlayListVk)playlist).SortByDate();
                else
                    if (sortCombobox.SelectedIndex == 1)
                        ((PlayListVk)playlist).SortByName();

                FillListBox((PlayListVk)playlist, listBox, false, true, true);
                if (currTrack != null)
                {
                    ((PlayListVk)playlist).SelectTrackByAid(selTrack);
                    listBox.SelectedIndex = ((PlayListVk)playlist).SelTrack;
                }
            }
        }

        public static string RemoveOtherSymbols(string inputStr)
        {
            string outputStr = "";
            for (int i = 0; i < inputStr.Length; i++)
            {
                if (i + 4 < inputStr.Length && inputStr[i] == '&' && inputStr[i + 1] == 'a' && inputStr[i + 2] == 'm' && inputStr[i + 3] == 'p' && inputStr[i + 4] == ';')
                {
                    if (i - 1 >= 0 && inputStr[i - 1] != ' ')
                        outputStr += " " + inputStr[i];
                    else
                        outputStr += inputStr[i];
                    continue;
                }
                else
                    if (i - 4 >= 0 && inputStr[i - 4] == '&' && inputStr[i - 3] == 'a' && inputStr[i - 2] == 'm' && inputStr[i - 1] == 'p' && inputStr[i] == ';')
                    {
                        if (i + 1 < inputStr.Length && inputStr[i + 1] != ' ')
                            outputStr += inputStr[i] + " ";
                        else
                            outputStr += inputStr[i];
                        continue;
                    }
                    else
                        if (Char.IsLetterOrDigit(inputStr[i]) || Char.IsWhiteSpace(inputStr[i]))
                            outputStr += inputStr[i];
            }
            return outputStr;
        }

        private async void updateButton_Click(object sender, RoutedEventArgs e)
        {
            bool isPlaying = playlistAll == playlist;
            var currTrack = ((PlayListVk)playlistAll).GetCurrentTrackVK();
            string selTrack = "";
            if (currTrack != null)
                selTrack = currTrack.aid;
            listBox.Items.Clear();
            tracksCountLabel.Content = "0 треків";
            playlistAll = new PlayListVk();
            await Task.Factory.StartNew(() => playlistAll.DownloadTracks(new string[] { sett.UserId, sett.VKToken }));

            tracksCountLabel.Content = playlistAll.Count() + " треків";

            FillListBox((PlayListVk)playlistAll, listBox, false, true, true);

            if (isPlaying)
                playlist = playlistAll;

            if (currTrack != null)
            {
                ((PlayListVk)playlistAll).SelectTrackByAid(selTrack);
                listBox.SelectedIndex = ((PlayListVk)playlistAll).SelTrack;
            }

        }


        private void volumeBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ChangeVolume((int)e.GetPosition(volumeBar).X);
        }

        private void volumeBar_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                volumeBar.Value -= 3;
            }
            else
                volumeBar.Value += 3;

            player.ChangeVolume(volumeBar.Value / 100);
        }

        private void volumeBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                ChangeVolume((int)e.GetPosition(volumeBar).X);
        }

        private void ChangeVolume(int x)
        {
            double dblValue;
            int point = x;
            dblValue = (point / (double)volumeBar.ActualWidth);

            volumeBar.Value = (int)((point / (double)volumeBar.ActualWidth) * 100);

            sett.VolumeLevel = volumeBar.Value / 100;

            player.ChangeVolume(sett.VolumeLevel);

        }

        private void playlistButton_Click(object sender, RoutedEventArgs e)
        {
            if (playlistButton.IsChecked == true)
            {
                this.Height = 600;
                taskInfo.ThumbnailClipMargin = new Thickness(0, 35, 650, 420);
            }
            if (playlistButton.IsChecked == false)
            {
                this.Height = 190;
                taskInfo.ThumbnailClipMargin = new Thickness(0, 35, 650, 0);
            }
            sett.EnableRepeating = ((ToggleButton)sender).IsChecked ?? true;
        }

        private void logInLastButton_Click(object sender, RoutedEventArgs e)
        {
            if (sett.LastSessionKey == "")
                GetAuthLast();
        }


        #region LastFM Scrobbling
        string ApiKey = "b7d62b44095bbe482030e12b4aa33572";
        string mySecret = "dd068a460a815ca350b125d3ae388e42";
        private void GetAuthLast()
        {
            // создаём объект HttpWebRequest через статический метод Create класса WebRequest, явно приводим результат к HttpWebRequest. В параметрах указываем страницу, которая указана в API, в качестве параметров - method=auth.gettoken и наш API Key


            HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/?method=auth.gettoken&api_key=" + ApiKey);

            // получаем ответ сервера
            HttpWebResponse tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();

            // и полностью считываем его в строку
            string tokenResult = new StreamReader(tokenResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();

            // извлекаем то, что нам нужно. Можно сделать и через парсинг XML (видимо, я о нём ещё не знал в тот момент, когда писал этот код).
            string token = String.Empty;
            for (int i = tokenResult.IndexOf("<token>") + 7; i < tokenResult.IndexOf("</token"); i++)
            {
                token += tokenResult[i];
            }

            // запускаем в браузере по умолчанию страницу http://www.last.fm/api/auth/ c параметрами API Key и только что полученным токеном)
            Process s = Process.Start("http://www.last.fm/api/auth/?api_key=" + ApiKey + "&token=" + token);

            // запускается страница, где у пользователя спрашивается, можно ли разрешить данному приложению доступ к профилю.

            // ждём подтверждения от пользователя
            System.Windows.Forms.DialogResult d = System.Windows.Forms.MessageBox.Show("Вы подтвердили доступ?", "Подтверждение", System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Question);

            // если пользователь дал согласие
            if (d == System.Windows.Forms.DialogResult.OK)
            {
                // создаём сигнатуру для получения сессии (указываем API Key, метод, токен и наш секретный ключ, всё это без символов '&' и '='
                string tmp = "api_key" + ApiKey + "methodauth.getsessiontoken" + token + mySecret;

                // хешируем это алгоритмом MD5 (думаю, у вас не будет проблем найти его в Интернете)
                string sig = GetMD5(tmp);

                // получаем сессию похожим способом
                HttpWebRequest sessionRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/?method=auth.getsession&token=" + token + "&api_key=" + ApiKey + "&api_sig=" + sig);
                // уже не помню, зачем это свойство выставлять в true, но это обязательно. Зачем-то им нужно перенаправление.
                sessionRequest.AllowAutoRedirect = true;

                // получаем ответ
                HttpWebResponse sessionResponse = (HttpWebResponse)sessionRequest.GetResponse();
                string sessionResult = new StreamReader(sessionResponse.GetResponseStream(),
                                               Encoding.UTF8).ReadToEnd();

                // извлечение сессии (опять же проще использовать XML парсер)
                for (int i = sessionResult.IndexOf("<key>") + 5; i < sessionResult.IndexOf("</key>"); i++)
                {
                    sett.LastSessionKey += sessionResult[i];
                }
            }
        }

        private string GetMD5(string input)
        {
            // создаем объект этого класса. Отмечу, что он создается не через new, а вызовом метода Create
            MD5 md5Hasher = MD5.Create();

            // Преобразуем входную строку в массив байт и вычисляем хэш
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Создаем новый Stringbuilder (Изменяемую строку) для набора байт
            StringBuilder sBuilder = new StringBuilder();

            // Преобразуем каждый байт хэша в шестнадцатеричную строку
            for (int i = 0; i < data.Length; i++)
            {
                //указывает, что нужно преобразовать элемент в шестнадцатиричную строку длиной в два символа
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }

        private void ScrobbleTrack(string track, string artist)
        {
            track = RemoveOtherSymbols(track);
            artist = RemoveOtherSymbols(artist);
            // узнаем UNIX-время для текущего момента
            TimeSpan rtime = DateTime.Now - (new DateTime(1970, 1, 1, 0, 0, 0));
            TimeSpan t1 = new TimeSpan(3, 0, 0);
            rtime -= t1; // вычитаем три часа, чтобы не было несоответствия из-за разницы в часовых поясах
            // получаем количество секунд
            int timestamp = (int)rtime.TotalSeconds;

            // формируем строку запроса
            string submissionReqString = String.Empty;

            //добавляем параметры (указываем метод, сессию и API Key):
            submissionReqString += "method=track.scrobble&sk=" + sett.LastSessionKey + "&api_key=" + ApiKey;

            //добавляем только обязательную информацию о треке (исполнитель, трек, время прослушивания, альбом), кодируя их с помощью статического метода UrlEncode класса HttpUtility.
            submissionReqString += "&artist=" + WebUtility.UrlEncode(artist);
            submissionReqString += "&track=" + WebUtility.UrlEncode(track);
            submissionReqString += "&timestamp=" + timestamp.ToString(); // в этой строке не должно быть пробела между & и t. Просто почему-то Хабр неправильно отображает этот участок, если пробел убрать.
            //submissionReqString += "&album=" + HttpUtility.UrlEncode(album);

            // формируем сигнатуру (параметры должны записываться сплошняком (без символов '&' и '=' и в алфавитном порядке):
            string signature = String.Empty;

            // сначала добавляем альбом
            //signature += "album" + album;

            // потом API Key
            signature += "api_key" + ApiKey;

            // исполнитель		   
            signature += "artist" + artist;

            // метод и ключ сессии
            signature += "methodtrack.scrobblesk" + sett.LastSessionKey;

            // время
            signature += "timestamp" + timestamp;

            // имя трека
            signature += "track" + track;

            // добавляем секретный код в конец
            signature += mySecret;

            // добавляем сформированную и захешированную MD5 сигнатуру к строке запроса
            submissionReqString += "&api_sig=" + GetMD5(signature);

            // и на этот раз делаем POST запрос на нужную страницу
            HttpWebRequest submissionRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/"); // адрес запроса без параметров

            // очень важная строка. Долго я мучался, пока не выяснил, что она обязательно должна быть
            submissionRequest.ServicePoint.Expect100Continue = false;

            // Настраиваем параметры запроса
            submissionRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.131 Safari/537.36";
            // Указываем метод отправки данных скрипту, в случае с POST обязательно
            submissionRequest.Method = "POST";
            // В случае с POST обязательная строка
            submissionRequest.ContentType = "application/x-www-form-urlencoded";

            // ставим таймаут, чтобы программа не повисла при неудаче обращения к серверу, а выкинула Exception
            submissionRequest.Timeout = 6000;

            // Преобразуем данные в соответствующую кодировку, получаем массив байтов из строки с параметрами (UTF8 обязательно)
            byte[] EncodedPostParams = Encoding.UTF8.GetBytes(submissionReqString);
            submissionRequest.ContentLength = EncodedPostParams.Length;

            // Записываем данные в поток запроса (массив байтов, откуда начинаем, сколько записываем)
            submissionRequest.GetRequestStream().Write(EncodedPostParams, 0, EncodedPostParams.Length);
            // закрываем поток
            submissionRequest.GetRequestStream().Close();

            // получаем ответ сервера
            HttpWebResponse submissionResponse = (HttpWebResponse)submissionRequest.GetResponse();

            // считываем поток ответа
            string submissionResult = new StreamReader(submissionResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            // разбор полётов. Если ответ не содержит status="ok", то дело плохо, выкидываем Exception и где-нибудь ловим его.
            if (!submissionResult.Contains("status=\"ok\""))
                throw new Exception("Треки не отправлены! Причина - " + submissionResult);

            // иначе всё хорошо, выходим из метода и оповещаем пользователя, что трек заскробблен.
        }
        private void LastUpdateNowTrack(string track, string artist)
        {
            track = RemoveOtherSymbols(track);
            artist = RemoveOtherSymbols(artist);
            // узнаем UNIX-время для текущего момента
            TimeSpan rtime = DateTime.Now - (new DateTime(1970, 1, 1, 0, 0, 0));
            TimeSpan t1 = new TimeSpan(3, 0, 0);
            rtime -= t1; // вычитаем три часа, чтобы не было несоответствия из-за разницы в часовых поясах
            // получаем количество секунд
            int timestamp = (int)rtime.TotalSeconds;

            // формируем строку запроса
            string submissionReqString = String.Empty;

            //добавляем параметры (указываем метод, сессию и API Key):
            submissionReqString += "method=track.updateNowPlaying&sk=" + sett.LastSessionKey + "&api_key=" + ApiKey;

            //добавляем только обязательную информацию о треке (исполнитель, трек, время прослушивания, альбом), кодируя их с помощью статического метода UrlEncode класса HttpUtility.
            submissionReqString += "&artist=" + WebUtility.UrlEncode(artist);
            submissionReqString += "&track=" + WebUtility.UrlEncode(track);
            // в этой строке не должно быть пробела между & и t. Просто почему-то Хабр неправильно отображает этот участок, если пробел убрать.
            //submissionReqString += "&album=" + HttpUtility.UrlEncode(album);

            // формируем сигнатуру (параметры должны записываться сплошняком (без символов '&' и '=' и в алфавитном порядке):
            string signature = String.Empty;

            // сначала добавляем альбом
            //signature += "album" + album;

            // потом API Key
            signature += "api_key" + ApiKey;

            // исполнитель		   
            signature += "artist" + artist;

            // метод и ключ сессии
            signature += "methodtrack.updateNowPlayingsk" + sett.LastSessionKey;

            // имя трека
            signature += "track" + track;

            // добавляем секретный код в конец
            signature += mySecret;

            // добавляем сформированную и захешированную MD5 сигнатуру к строке запроса
            submissionReqString += "&api_sig=" + GetMD5(signature);

            // и на этот раз делаем POST запрос на нужную страницу
            HttpWebRequest submissionRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/"); // адрес запроса без параметров

            // очень важная строка. Долго я мучался, пока не выяснил, что она обязательно должна быть
            submissionRequest.ServicePoint.Expect100Continue = false;

            // Настраиваем параметры запроса
            submissionRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.131 Safari/537.36";
            // Указываем метод отправки данных скрипту, в случае с POST обязательно
            submissionRequest.Method = "POST";
            // В случае с POST обязательная строка
            submissionRequest.ContentType = "application/x-www-form-urlencoded";

            // ставим таймаут, чтобы программа не повисла при неудаче обращения к серверу, а выкинула Exception
            submissionRequest.Timeout = 6000;

            // Преобразуем данные в соответствующую кодировку, получаем массив байтов из строки с параметрами (UTF8 обязательно)
            byte[] EncodedPostParams = Encoding.UTF8.GetBytes(submissionReqString);
            submissionRequest.ContentLength = EncodedPostParams.Length;

            // Записываем данные в поток запроса (массив байтов, откуда начинаем, сколько записываем)
            submissionRequest.GetRequestStream().Write(EncodedPostParams, 0, EncodedPostParams.Length);
            // закрываем поток
            submissionRequest.GetRequestStream().Close();

            // получаем ответ сервера
            HttpWebResponse submissionResponse = (HttpWebResponse)submissionRequest.GetResponse();

            // считываем поток ответа
            string submissionResult = new StreamReader(submissionResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            // разбор полётов. Если ответ не содержит status="ok", то дело плохо, выкидываем Exception и где-нибудь ловим его.
            if (!submissionResult.Contains("status=\"ok\""))
                throw new Exception("Треки не отправлены! Причина - " + submissionResult);

            // иначе всё хорошо, выходим из метода и оповещаем пользователя, что трек заскробблен.
        }
        #endregion

        private void albumsCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (albums != null && albumsCombobox.SelectedIndex >= 0)
            {
                albums.SelectAlbum(albumsCombobox.SelectedIndex, sett.UserId, sett.VKToken);
                tracksCountAlbumLabel.Content = albums.GetSelectedAlbum().playlist.Count() + " треків";
                FillListBox(albums.GetSelectedAlbum().playlist, listAlbumBox, false, true, false);
                Button_Click_3(this, new RoutedEventArgs());
            }
        }

        private void listAlbumBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listAlbumBox.ScrollIntoView(listAlbumBox.SelectedItem);
        }

        private void sortInAlbumCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (albums != null)
            {
                var pls = albums.GetSelectedAlbum().playlist;
                if (pls != null)
                {
                    var currTrack = pls.GetCurrentTrackVK();
                    string selTrack = "";
                    if (currTrack != null)
                        selTrack = currTrack.aid;

                    if (sortInAlbumCombobox.SelectedIndex == 0)
                        pls.SortByDate();
                    else
                        if (sortInAlbumCombobox.SelectedIndex == 1)
                            pls.SortByName();

                    FillListBox(pls, listAlbumBox, false, true, false);
                    if (currTrack != null)
                    {
                        pls.SelectTrackByAid(selTrack);
                        listAlbumBox.SelectedIndex = pls.SelTrack;
                    }
                }
            }
        }

        private void reverseInAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            var pls = albums.GetSelectedAlbum().playlist;
            var currTrack = pls.GetCurrentTrackVK();
            string selTrack = "";
            if (currTrack != null)
                selTrack = currTrack.aid;
            if (reverseInAlbumButton.IsChecked == true)
            {
                pls.ReverseOn();
            }
            else
                pls.ReverseOff();
            FillListBox(pls, listAlbumBox, false, true, false);
            if (currTrack != null)
            {
                pls.SelectTrackByAid(selTrack);
                listAlbumBox.SelectedIndex = pls.SelTrack;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            listAlbumBox.SelectedIndex = -1;
            listAlbumBox.SelectedIndex = albums.GetSelectedAlbum().playlist.SelTrack;
        }

        private void updateAlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            var curAlbum = albums.GetSelectedAlbum();
            KeyValuePair<string, string>[] selectedTracks = null;
            selectedTracks = albums.GetSelectedTracksForAlbum();
            bool isAlbumPlaying = curAlbum != null ? albums.GetSelectedAlbum().playlist == playlist : false;
            listAlbumBox.Items.Clear();
            tracksCountAlbumLabel.Content = "0 треків";

            albums = new Albums();

            albums.DownloadAlbums(new string[] { sett.UserId, sett.VKToken });

            FillAlbums(albums);

            if (curAlbum != null)
            {
                var index = Array.IndexOf(albums.GetAlbumsIds(), curAlbum.Id, 0);
                albumsCombobox.SelectedIndex = index;
                albumsCombobox_SelectionChanged(null, null);
                curAlbum = albums.GetSelectedAlbum();
                tracksCountAlbumLabel.Content = curAlbum.playlist.Count() + " треків";
            }
            else
                if (albums.Count() > 0)
                {
                    albumsCombobox.SelectedIndex = 0;
                    albumsCombobox_SelectionChanged(null, null);
                    curAlbum = albums.GetSelectedAlbum();
                    tracksCountAlbumLabel.Content = albums.GetSelectedAlbum().playlist.Count() + " треків";
                }
            if (selectedTracks != null)
                albums.SetSelectedTracksForAlbum(selectedTracks);


            if (curAlbum != null)
                FillListBox(albums.GetSelectedAlbum().playlist, listAlbumBox, false, true, false);
            else
                listAlbumBox.Items.Clear();

            if (curAlbum != null)
                listAlbumBox.SelectedIndex = albums.GetSelectedAlbum().playlist.SelTrack;

            if (isAlbumPlaying)
                playlist = albums.GetSelectedAlbum().playlist;
        }

        private void searchInAlbumBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (searchInAlbumBox.Text.Trim() != "" && listAlbumBox.SelectedIndex != -1)
                {
                    DoubleAlbumListBoxClick();
                }
        }

        private void DoubleAlbumListBoxClick()
        {
            if (playlist != albums.GetSelectedAlbum().playlist)
                cherga.Clear();
            playlist = albums.GetSelectedAlbum().playlist;
            playlist.SelTrack = listAlbumBox.SelectedIndex;
            if (sett.EnableShuffling)
                if (cherga.Count > 0)
                {
                    if (cherga[cherga.Count - 1] != playlist.SelTrack)
                        cherga.Add(playlist.SelTrack);
                }
                else
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void searchInAlbumBox_KeyUp(object sender, KeyEventArgs e)
        {
            var pls = albums.GetSelectedAlbum().playlist;
            if ((e.Key < Key.A && e.Key > Key.Z) || (e.Key < Key.D0 && e.Key > Key.D9))
            {
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Enter)
                if (e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl)
                {
                    if (pls != null && pls.Count() > 0)
                    {
                        var searchText = searchInAlbumBox.Text.Trim();
                        if (searchText == "")
                        {
                            listAlbumBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                        for (int i = 0; i < pls.Count(); i++)
                        {
                            if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                listAlbumBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
        }

        private void clearInAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            searchInAlbumBox.Clear();
            listAlbumBox.SelectedIndex = albums.GetSelectedAlbum().playlist.SelTrack;
        }

        private void searchInAlbumPrev_Click(object sender, RoutedEventArgs e)
        {
            var pls = albums.GetSelectedAlbum().playlist;
            var searchText = searchInAlbumBox.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listAlbumBox.SelectedIndex != -1)
                {
                    for (int i = listAlbumBox.SelectedIndex - 1; i > 0; i--)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listAlbumBox.SelectedIndex = i;
                            break;
                        }
                        if (i == 0)
                        {
                            listAlbumBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInAlbumBox.Focus();


        }

        private void searchInAlbumNext_Click(object sender, RoutedEventArgs e)
        {
            var pls = albums.GetSelectedAlbum().playlist;
            var searchText = searchInAlbumBox.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listAlbumBox.SelectedIndex != -1)
                {
                    for (int i = listAlbumBox.SelectedIndex + 1; i < listAlbumBox.Items.Count; i++)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listAlbumBox.SelectedIndex = i;
                            break;
                        }
                        if (i == listAlbumBox.Items.Count)
                        {
                            listAlbumBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInAlbumBox.Focus();
        }

        private void listAlbumBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!listAlbumBox.IsFocused)
                listAlbumBox.Focus();
        }

        private void listAlbumBox_KeyUp(object sender, KeyEventArgs e)
        {
            var pls = albums.GetSelectedAlbum().playlist;
            if (e.Key == Key.Delete && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                int selIndex = listAlbumBox.SelectedIndex;
                pls.Remove(selIndex);
                if (selIndex <= pls.SelTrack)
                    pls.SelTrack--;
                listAlbumBox.Items.RemoveAt(selIndex);
                if (sett.EnableShuffling == true)
                {
                    if (cherga.Contains(selIndex))
                        cherga.Remove(selIndex);
                }
                listAlbumBox.SelectedIndex = selIndex;
            }
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                int selIndex = listAlbumBox.SelectedIndex;

                DeleteTrack(selIndex);


                if (pls.SelTrack == selIndex)
                {

                    if (sett.EnableShuffling == true)
                    {
                        if (cherga.Contains(selIndex))
                            cherga.Remove(selIndex);
                    }
                    PlayTrack();
                }
                if (selIndex < pls.SelTrack)
                    pls.SelTrack--;
                listAlbumBox.SelectedIndex = selIndex;
            }
        }

        private void listAlbumBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoubleAlbumListBoxClick();
            }
        }

        private void offlineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sett.SetOffline() != "1")
                MessageBox.Show("Проблема з'єднання з vk.com");
        }

        private void loadFriendList()
        {
            users = new FriendList(sett.UserId, sett.VKToken, GetAuth);
            FillUsers(listUserBox);
            users.OnUsersUpdated += users_OnUsersUpdated;
            //
            friendCombobox.Items.Clear();
            friendCombobox.Items.Add("ALL");
            foreach (var elm in users.GetGettedUsers())
            {
                friendCombobox.Items.Add(elm.first_name + " " + elm.last_name);
            }
        }

        void users_OnUsersUpdated()
        {
            string selUser = users.GetSelectedUserId();
            if (listUserBox == null || listUserBox.Items.Count == 0)
                FillUsers(listUserBox);
            else
            {
                //updating
                FillUsers(listUserBox);
            }
            if (selUser.Trim().Length > 0)
                listUserBox.SelectedIndex = users.SelectUserById(selUser);
        }

        int activeUser = -1;
        private void listUserBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listUserBox.SelectedIndex >= 0)
            {
                if (users.CanGetTracks(listUserBox.SelectedIndex))
                {
                    this.users.SelectUser(listUserBox.SelectedIndex);
                    tracksUserCountLabel.Content = users.GetSelectedUserPlaylist().Count() + " треків " + users.GetCurrentUserName();
                    if (activeUser != listUserBox.SelectedIndex)
                    {
                        FillListBox(users.GetSelectedUserPlaylist(), listUserTracksBox, true, false, false);
                        users.CurrentUserFilledTracks();
                        GetCurrentUserTrack_Click(sender, e);
                    }
                    else
                        if (!users.IsCurrentUserFillTracks)
                        {
                            FillListBox(users.GetSelectedUserPlaylist(), listUserTracksBox, true, false, false);
                            users.CurrentUserFilledTracks();
                            GetCurrentUserTrack_Click(sender, e);
                        }
                }
                else
                {
                    listUserBox.SelectedIndex = users.GetSelectedUserIndex();
                    MessageBox.Show("Користувач заборонив переглядати аудіозаписи");
                }
                activeUser = listUserBox.SelectedIndex;
            }
        }

        private void updateuserTracksButton_Click(object sender, RoutedEventArgs e)
        {
            if (users.IsSelected)
                if (users.CanGetTracks())
                {
                    this.users.UpdateUserTracks();
                    tracksUserCountLabel.Content = users.GetSelectedUserPlaylist().Count() + " треків " + users.GetCurrentUserName();
                    FillListBox(users.GetSelectedUserPlaylist(), listUserTracksBox, true, false, false);
                    users.CurrentUserFilledTracks();
                    GetCurrentUserTrack_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Користувач заборонив переглядати аудіозаписи");
                }
        }

        private void sortInUserTracksCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (users != null && users.IsSelected)
            {
                PlayListVk pls = users.GetSelectedUserPlaylist();
                if (pls != null)
                {
                    var currTrack = pls.GetCurrentTrackVK();
                    string selTrack = "";
                    if (currTrack != null)
                        selTrack = currTrack.aid;

                    if (sortInUserTracksCombobox.SelectedIndex == 0)
                        pls.SortByDate();
                    else
                        if (sortInUserTracksCombobox.SelectedIndex == 1)
                            pls.SortByName();

                    FillListBox(pls, listUserTracksBox, true, false, false);
                    users.CurrentUserFilledTracks();
                    if (currTrack != null)
                    {
                        pls.SelectTrackByAid(selTrack);
                        listUserTracksBox.SelectedIndex = pls.SelTrack;
                    }
                }
            }
        }

        private void reverseInUserTracksButton_Click(object sender, RoutedEventArgs e)
        {
            if (users != null && users.IsSelected)
            {
                PlayListVk pls = users.GetSelectedUserPlaylist();
                var currTrack = pls.GetCurrentTrackVK();
                string selTrack = "";
                if (currTrack != null)
                    selTrack = currTrack.aid;
                if (reverseInUserTracksButton.IsChecked == true)
                {
                    pls.ReverseOn();
                }
                else
                    pls.ReverseOff();
                FillListBox(pls, listAlbumBox, true, false, false);
                users.CurrentUserFilledTracks();
                if (currTrack != null)
                {
                    pls.SelectTrackByAid(selTrack);
                    listUserTracksBox.SelectedIndex = pls.SelTrack;
                }
            }
        }

        private void GetCurrentUserTrack_Click(object sender, RoutedEventArgs e)
        {
            if (users != null && users.IsSelected)
            {
                listUserTracksBox.SelectedIndex = -1;
                listUserTracksBox.SelectedIndex = users.GetSelectedUserPlaylist().SelTrack;
            }
        }

        private void listUserTracksBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listUserTracksBox.ScrollIntoView(listUserTracksBox.SelectedItem);
        }

        private void searchInUserTracksBox_KeyUp(object sender, KeyEventArgs e)
        {
            var pls = users.GetSelectedUserPlaylist();
            if ((e.Key < Key.A && e.Key > Key.Z) || (e.Key < Key.D0 && e.Key > Key.D9))
            {
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Enter)
                if (e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl)
                {
                    if (pls != null && pls.Count() > 0)
                    {
                        var searchText = searchInUserTracksBox.Text.Trim();
                        if (searchText == "")
                        {
                            listUserTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                        for (int i = 0; i < pls.Count(); i++)
                        {
                            if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                listUserTracksBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
        }

        private void searchInUserBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (searchInUserTracksBox.Text.Trim() != "" && listUserBox.SelectedIndex != -1)
                {
                    DoubleUserListBoxClick();
                }
        }
        private void DoubleUserListBoxClick()
        {
            if (playlist != users.GetSelectedUserPlaylist())
            {
                cherga.Clear();
                playlist = users.GetSelectedUserPlaylist();
            }
            playlist.SelTrack = listUserBox.SelectedIndex;
            if (sett.EnableShuffling)
                if (cherga.Count > 0)
                {
                    if (cherga[cherga.Count - 1] != playlist.SelTrack)
                        cherga.Add(playlist.SelTrack);
                }
                else
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void clearInSearchUserTracksButton_Click(object sender, RoutedEventArgs e)
        {
            searchInUserTracksBox.Clear();
            listUserTracksBox.SelectedIndex = users.GetSelectedUserPlaylist().SelTrack;
        }

        private void searchInUserTracksPrev_Click(object sender, RoutedEventArgs e)
        {
            var pls = users.GetSelectedUserPlaylist();
            var searchText = searchInUserTracksBox.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listUserTracksBox.SelectedIndex != -1)
                {
                    for (int i = listUserTracksBox.SelectedIndex - 1; i > 0; i--)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listUserTracksBox.SelectedIndex = i;
                            break;
                        }
                        if (i == 0)
                        {
                            listUserTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInUserTracksBox.Focus();
        }

        private void searchInUserTracksNext_Click(object sender, RoutedEventArgs e)
        {
            var pls = users.GetSelectedUserPlaylist();
            var searchText = searchInUserTracksBox.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listUserTracksBox.SelectedIndex != -1)
                {
                    for (int i = listUserTracksBox.SelectedIndex + 1; i < listUserTracksBox.Items.Count; i++)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listUserTracksBox.SelectedIndex = i;
                            break;
                        }
                        if (i == listUserTracksBox.Items.Count)
                        {
                            listUserTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInUserTracksBox.Focus();
        }

        private void friendCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (friendCombobox.SelectedItem != null)
            {
                string selObj = (string)friendCombobox.SelectedItem;
                List<string> arrUsers = new List<string>();
                if (selObj == "ALL")
                    foreach (User user in users.GetUsers())
                    {
                        arrUsers.Add(user.id);
                    }
                else
                {
                    arrUsers.Add(users.SelectUserIdByName(selObj));
                }
                friendCombobox.SelectedIndex = -1;
                try
                {
                    users.SendTrackToFriends(playlist.GetCurrentTrackVK(), arrUsers.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        private void ClearQueryButton_Click(object sender, RoutedEventArgs e)
        {
            searchQueryTextBox.Clear();
        }

        private void SearchQueryButton_Click(object sender, RoutedEventArgs e)
        {
            search.SearchTracks(searchQueryTextBox.Text.Trim());
            searchedCountLabel.Content = search.GetSearchedCount() + " треків " + "\"" + search.Query + "\"";
            FillListBox(search.Playlist, listSearchedTracksBox, true, false, false);
            listSearchedTracksBox.SelectedIndex = -1;
            if (listSearchedTracksBox.Items.Count > 0)
            {
                listSearchedTracksBox.SelectedIndex = 0;
                listSearchedTracksBox.SelectedIndex = -1;
            }
        }

        private void listSearchedTracksBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listSearchedTracksBox.ScrollIntoView(listSearchedTracksBox.SelectedItem);
        }

        private void GetCurrentSearchTrack_Click(object sender, RoutedEventArgs e)
        {
            if (search != null && search.Query.Length > 0)
            {
                listSearchedTracksBox.SelectedIndex = -1;
                listSearchedTracksBox.SelectedIndex = search.Playlist.SelTrack;
            }
        }

        private void searchQueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchQueryButton_Click(null, null);
        }

        private void searchInSearchedTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var pls = search.Playlist;
            if ((e.Key < Key.A && e.Key > Key.Z) || (e.Key < Key.D0 && e.Key > Key.D9))
            {
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Enter)
                if (e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl)
                {
                    if (pls != null && pls.Count() > 0)
                    {
                        var searchText = searchInSearchedTextBox.Text.Trim();
                        if (searchText == "")
                        {
                            listSearchedTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                        for (int i = 0; i < pls.Count(); i++)
                        {
                            if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                listSearchedTracksBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
        }

        private void searchInSearchedTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (searchInSearchedTextBox.Text.Trim() != "" && listSearchedTracksBox.SelectedIndex != -1)
                {
                    DoubleSearchListBoxClick();
                }
        }
        private void DoubleSearchListBoxClick()
        {
            if (playlist != search.Playlist)
            {
                cherga.Clear();
                playlist = search.Playlist;
            }
            playlist.SelTrack = listSearchedTracksBox.SelectedIndex;
            if (sett.EnableShuffling)
                if (cherga.Count > 0)
                {
                    if (cherga[cherga.Count - 1] != playlist.SelTrack)
                        cherga.Add(playlist.SelTrack);
                }
                else
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void SearchInSearchedClear_Click(object sender, RoutedEventArgs e)
        {
            searchInSearchedTextBox.Clear();
            listSearchedTracksBox.SelectedIndex = search.Playlist.SelTrack;
        }

        private void SearchInSearchedPrev_Click(object sender, RoutedEventArgs e)
        {
            var pls = search.Playlist;
            var searchText = searchInSearchedTextBox.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listSearchedTracksBox.SelectedIndex != -1)
                {
                    for (int i = listSearchedTracksBox.SelectedIndex - 1; i > 0; i--)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listSearchedTracksBox.SelectedIndex = i;
                            break;
                        }
                        if (i == 0)
                        {
                            listSearchedTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInSearchedTextBox.Focus();
        }

        private void SearchInSearchedNext_Click(object sender, RoutedEventArgs e)
        {
            var pls = search.Playlist;
            var searchText = searchInSearchedTextBox.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listSearchedTracksBox.SelectedIndex != -1)
                {
                    for (int i = listSearchedTracksBox.SelectedIndex + 1; i < listSearchedTracksBox.Items.Count; i++)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listSearchedTracksBox.SelectedIndex = i;
                            break;
                        }
                        if (i == listSearchedTracksBox.Items.Count)
                        {
                            listSearchedTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInSearchedTextBox.Focus();
        }

        private async void UpdateRecommendations()
        {
            await Task.Factory.StartNew(() => recommendations.LoadReccomendations());
            FillListBox(recommendations.Playlist, listRecommedTracksBox, true, false, false);
            recommedCountLabel.Content = recommendations.GetSearchedCount() + " треків";
        }

        private void RecommendCurrentTrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (recommendations.Playlist.SelTrack != -1)
            {
                listRecommedTracksBox.SelectedIndex = -1;
                listRecommedTracksBox.SelectedIndex = recommendations.Playlist.SelTrack;
            }
        }

        private void RecommendUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateRecommendations();
            listRecommedTracksBox.SelectedIndex = -1;
            if (listRecommedTracksBox.Items.Count > 0)
            {
                listRecommedTracksBox.SelectedIndex = 0;
                listRecommedTracksBox.SelectedIndex = -1;
            }
        }

        private void searchInRecommed_KeyUp(object sender, KeyEventArgs e)
        {
            var pls = recommendations.Playlist;
            if ((e.Key < Key.A && e.Key > Key.Z) || (e.Key < Key.D0 && e.Key > Key.D9))
            {
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Enter)
                if (e.Key != Key.LeftAlt && e.Key != Key.LeftCtrl)
                {
                    if (pls != null && pls.Count() > 0)
                    {
                        var searchText = searchInRecommed.Text.Trim();
                        if (searchText == "")
                        {
                            listRecommedTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                        for (int i = 0; i < pls.Count(); i++)
                        {
                            if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                listRecommedTracksBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
        }

        private void searchInRecommed_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (searchInRecommed.Text.Trim() != "" && listRecommedTracksBox.SelectedIndex != -1)
                {
                    DoubleRecommListBoxClick();
                }
        }

        private void DoubleRecommListBoxClick()
        {
            if (playlist != recommendations.Playlist)
            {
                cherga.Clear();
                playlist = recommendations.Playlist;
            }
            playlist.SelTrack = listRecommedTracksBox.SelectedIndex;
            if (sett.EnableShuffling)
                if (cherga.Count > 0)
                {
                    if (cherga[cherga.Count - 1] != playlist.SelTrack)
                        cherga.Add(playlist.SelTrack);
                }
                else
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void searchInRecommedClear_Click(object sender, RoutedEventArgs e)
        {
            searchInRecommed.Clear();
            listRecommedTracksBox.SelectedIndex = recommendations.Playlist.SelTrack;
        }

        private void searchInRecommedPrev_Click(object sender, RoutedEventArgs e)
        {
            var pls = recommendations.Playlist;
            var searchText = searchInRecommed.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listRecommedTracksBox.SelectedIndex != -1)
                {
                    for (int i = listRecommedTracksBox.SelectedIndex - 1; i > 0; i--)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listRecommedTracksBox.SelectedIndex = i;
                            break;
                        }
                        if (i == 0)
                        {
                            listRecommedTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInRecommed.Focus();
        }

        private void searchInRecommedNext_Click(object sender, RoutedEventArgs e)
        {
            var pls = recommendations.Playlist;
            var searchText = searchInRecommed.Text.Trim();
            if (pls != null && pls.Count() > 0)
                if (searchText != "" && listRecommedTracksBox.SelectedIndex != -1)
                {
                    for (int i = listRecommedTracksBox.SelectedIndex + 1; i < listRecommedTracksBox.Items.Count; i++)
                    {
                        if (pls.GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listRecommedTracksBox.SelectedIndex = i;
                            break;
                        }
                        if (i == listRecommedTracksBox.Items.Count)
                        {
                            listRecommedTracksBox.SelectedIndex = pls.SelTrack;
                            return;
                        }
                    }
                }
            searchInRecommed.Focus();
        }

        private void listRecommedTracksBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listRecommedTracksBox.ScrollIntoView(listRecommedTracksBox.SelectedItem);
        }

        private void ShowNotiff(string text)
        {
            eventLabel.Content = text;
            eventLabel.Opacity = 1;
            Formms.Timer tim = new Formms.Timer();
            tim.Interval = 5000;
            tim.Tick += (s, e) =>
                {
                    new Thread(() =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                    {


                        eventLabel.Opacity -= 0.1;

                        return null;
                    }), null);
                            Thread.Sleep(100);
                        }
                    }).Start();
                    tim.Stop();
                };
            tim.Start();
        }

        private void AddAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            editingMode = false;
            popupAlbum.IsOpen = true;
            albumNameTextBox.Text = "";
        }

        private void DeleteAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            albums.DeleteCurrent(sett.VKToken);
            updateAlbumsButton_Click(sender, e);
        }

        private void EditAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            editingMode = true;
            popupAlbum.IsOpen = true;
            albumNameTextBox.Text = albums.GetSelectedAlbum().Title;
        }

        bool editingMode = false;
        private void SaveAlbumName_Click(object sender, RoutedEventArgs e)
        {
            string newName = albumNameTextBox.Text.Trim();
            if (newName.Length > 0)
            {
                if (editingMode)
                    albums.Edit(newName, sett.VKToken);
                else
                    albums.Add(newName, sett.VKToken);
                popupAlbum.IsOpen = false;
                updateAlbumsButton_Click(sender, e);
            }
        }
        AudioVK editedTrack = null;
        private void SaveTrackName_Click(object sender, RoutedEventArgs e)
        {
            if (editedTrack != null)
            {
                string trackName = trackNameTextBox.Text.Trim();
                string artistName = artistNameTextBox.Text.Trim();
                if (trackName.Length > 0 && artistName.Length > 0)
                {
                    playlist.Edit(editedTrack, trackName, artistName, sett.VKToken);
                    popupTrack.IsOpen = false;
                    updateButton_Click(sender, e);
                }
            }
        }

        private void checkBoxRepeat_Click(object sender, RoutedEventArgs e)
        {
            sett.EnableRepeating = ((ToggleButton)sender).IsChecked ?? true;
        }

        public void Dispose()
        {
            users.Dispose();
            if(globalKeyThread!=null && globalKeyThread.IsAlive)
            globalKeyThread.Abort();
            buttonNext.Dispose();
            buttonPlayPause.Dispose();
            buttonPrevious.Dispose();
            player.Dispose();
            timer.Dispose();
            timerScrobble.Dispose();
        }

        private void TabFriendsGotFocus(object sender, RoutedEventArgs e)
        {
            users.StartTimer();
        }

        private void TabFriendsLostFocus(object sender, RoutedEventArgs e)
        {
            users.StopTimer();
        }
    }
}

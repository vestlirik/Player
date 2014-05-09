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
using Formms=System.Windows.Forms;
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

namespace VkAudioWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //форма авторизации
        Auth auth;

        Player player;
        PlayList playlist;

        //время последнего нажатия горячей кнопки
        DateTime lastInput;

        bool showInVk = true;

        //очередь для случайного воспроизведения
        List<int> cherga = new List<int>();

        //Кнопки для панели задач
        private ThumbnailToolBarButton buttonPlayPause;
        private ThumbnailToolBarButton buttonNext;
        private ThumbnailToolBarButton buttonPrevious;
        TaskbarManager tbManager = TaskbarManager.Instance;

        Formms.Timer timer;


        public MainWindow()
        {
            InitializeComponent();

            timer = new System.Windows.Forms.Timer();
            timer.Tick+=timer_Tick;

            //this.Text = "Стоп";
            auth = new Auth();
            player = new Player();
            this.Focus();
            lastInput = DateTime.Now;

            timer.Stop();

            startTime.Content = startTime.Content = "0:00";
            progressBar.Value = 0;

            //громкость
            //trackBar2.ValueChanged += trackBar2_ValueChanged;
            //trackBar2.Value = player.Volume;
            //Attach the event handler of WMPengine
            player.StatusChanged += new Player.OnStatusUpdate(engine_StatusChanged);

            //taskbar
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

            
            //TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip
            //  (Process.GetCurrentProcess().MainWindowHandle, new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(10,10))); 
            #endregion


            //notifyIcon1.Visible = true;
            //notifyIcon1.Icon = SystemIcons.Hand;

            logInButton.Visibility = System.Windows.Visibility.Visible;
            logOutButton.Visibility = System.Windows.Visibility.Collapsed;
            usernameLabel.Visibility = System.Windows.Visibility.Collapsed;

           
        }

        public bool ShowBroadcast
        {
            get { return showInVk; }
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
            auth.Close();
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
            if (!auth.IsAuth)
            {
                ////Авторизация в вк
                auth.ShowDialog();
            }
                if (auth.IsAuth)
                {
                    try
                    {
                        ////загрузка данных о профиле
                        XmlDocument x = new XmlDocument();
                        x.Load("https://api.vk.com/method/getProfiles.xml?uid=" + auth.UserId + "&access_token=" + auth.Token);
                        // Парсим
                        var el = x.GetElementsByTagName("user")[0];
                        usernameLabel.Content = el.ChildNodes[1].InnerText + " " + el.ChildNodes[2].InnerText;
                        logInButton.Visibility = System.Windows.Visibility.Collapsed;
                        usernameLabel.Visibility = System.Windows.Visibility.Visible;
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
            if (auth.IsAuth)
            {
                playlist = new PlayListVk();
                await Task.Factory.StartNew(() => playlist.DownloadTracks(new string[] { auth.UserId, auth.Token }));

                tracksCountLabel.Content = playlist.Count()+" треків";

                FillListBox((PlayListVk)playlist);
                
            }
        }

        private void FillListBox(PlayListVk pls)
        {
            listBox.Items.Clear();
            foreach (var elm in pls.GetTrackListVK())
            {
                #region xaml listboxitem
                //<ListBoxItem Height="25px" HorizontalAlignment="Stretch" Margin="0,0,0,3">
                //            <Grid>
                //                <Grid.ColumnDefinitions>
                //                    <ColumnDefinition Width="410"/>
                //                    <ColumnDefinition Width="60"/>
                //                    <ColumnDefinition Width="70"/>
                //                    <ColumnDefinition Width="40"/>
                //                    <ColumnDefinition Width="70"/>
                //                    <ColumnDefinition Width="80"/>
                //                    <ColumnDefinition Width="40"/>
                //                </Grid.ColumnDefinitions>
                //                <Label Grid.Column="0" >Мері - Сестра</Label>
                //                <Label Grid.Column="1" >320 kbps</Label>
                //                <Label  Grid.Column="2">44 100 Hz</Label>
                //                <Label Grid.Column="3">4:09</Label>
                //                <Label Grid.Column="4">10,9 MB</Label>
                //                <Button Grid.Column="5" Style="{StaticResource roundedButton}">Скачати</Button>
                //                <Button Grid.Column="6"  Style="{StaticResource roundedButton}">X</Button>
                //            </Grid>
                //        </ListBoxItem> 
                #endregion
                ListBoxItem lstItem = new ListBoxItem();
                lstItem.Height = 30;
                lstItem.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                lstItem.Margin = new Thickness(0, 0, 0, 3);
                lstItem.Style = this.FindResource("lstStyle") as Style;

                #region creating grid for listboxItem
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(400) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(70) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });
                #endregion


                #region add elements in grid for listboxitem
                System.Windows.Controls.Label lbl = new System.Windows.Controls.Label();
                lbl.Content = elm.artist + " - " + elm.title;
                Grid.SetColumn(lbl, 0);
                grid.Children.Add(lbl);

                lbl = new System.Windows.Controls.Label();
                lbl.Content = "320" + " kbps";
                Grid.SetColumn(lbl, 1);
                grid.Children.Add(lbl);

                lbl = new System.Windows.Controls.Label();
                lbl.Content = "44100" + " Hz";
                Grid.SetColumn(lbl, 2);
                grid.Children.Add(lbl);

                lbl = new System.Windows.Controls.Label();
                lbl.Content = elm.DurationString;
                Grid.SetColumn(lbl, 3);
                grid.Children.Add(lbl);

                lbl = new System.Windows.Controls.Label();
                lbl.Name = "lbl" + listBox.Items.Count;

                //new Task((() =>
                //    {

                //        this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                //        {

                //            lbl.Content = String.Format("{0:0.00}", GetSize(elm.GetLocation)) + " MB";

                //            return null;

                //        }), null);

                //    }


                //    )).Start();

                Grid.SetColumn(lbl, 4);
                grid.Children.Add(lbl);

                Button btn = new Button();
                btn.Style = this.FindResource("roundedButton") as Style;
                btn.Content = "Скачати";

                btn.Click += (object send, RoutedEventArgs ee) =>
                {
                    DownloadFile(elm.GetLocation, elm.Name);
                };




                Grid.SetColumn(btn, 5);
                grid.Children.Add(btn);

                btn = new Button();
                btn.Style = this.FindResource("roundedButton") as Style;
                btn.Content = "X";
                Grid.SetColumn(btn, 6);
                grid.Children.Add(btn);
                #endregion

                lstItem.Content = grid;

                lstItem.MouseDoubleClick += lstItem_MouseDoubleClick;
                //lstItem.MouseEnter += lstItem_MouseEnter;

                listBox.Items.Add(lstItem);
            }
        }
               
        void lstItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist.SelTrack = listBox.SelectedIndex;
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
                if (checkBoxShuffle.IsChecked==true)
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

                    listBox.SelectedIndex = playlist.SelTrack;
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
                listBox.SelectedIndex = playlist.SelTrack;


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
            Audio currSong = null;
            var plstType = playlist.GetType();
            if (plstType.Name == "PlayListLocal")
            {
                currSong = ((PlayListLocal)playlist).GetCurrentTrack();
                player.AttachSong(currSong.GetLocation);
            }
            else if (plstType.Name == "PlayListVk")
            {
                currSong = ((PlayListVk)playlist).GetCurrentTrackVK();
                player.AttachUrlSong(currSong.GetLocation);
                //set status
                var audioId = ((AudioVK)currSong).owner_id + "_" + ((AudioVK)currSong).aid;
                if(ShowBroadcast)
                ((PlayListVk)playlist).SetStatus(audioId, auth.Token);
            }


            listBox.SelectedIndex = playlist.SelTrack;

            //if (plstType.Name == "PlayListVk")
            //    richTextBox1.Text = ((AudioVK)currSong).GetLirycs(auth.Token);
            //else
            //    richTextBox1.Clear();

            player.Play();


            titleLabel.Content = player.GetName(currSong.GetLocation);
            if (titleLabel.Content.ToString().Trim() == "")
                titleLabel.Content = currSong.Name;

            progressBar.Minimum = 0;
            if (plstType.Name == "PlayListLocal")
            {
                progressBar.Maximum = player.Duration(currSong.GetLocation);
                endTime.Content = player.DurationString(currSong.GetLocation);
            }
            else if (plstType.Name == "PlayListVk")
            {
                progressBar.Maximum = int.Parse(((AudioVK)currSong).duration);
                endTime.Content = ((AudioVK)currSong).DurationString;
            }
            songDataLabel.Content = player.Bitrate(currSong.GetLocation) + " КБ/с  " + player.Fraquency(currSong.GetLocation) + " KHz";

            timer.Enabled = true;

            this.headerLabel.Content = titleLabel.Content + " - Zeus";
            this.Title = titleLabel.Content + " - Zeus";

            var tmpStr = titleLabel.Content.ToString();
            var str = tmpStr.Length > 64 ? tmpStr.Substring(0, 64) : tmpStr;
            //notifyIcon1.ShowBalloonTip(500, "Наступний трек", str, ToolTipIcon.Info);

            //SetTaskbarthumbnail();

            GetAlbumArt(((AudioVK)currSong).artist, ((AudioVK)currSong).title);
        }

        private void GetAlbumArt(string artist, string title)
        {
            new Thread(() =>
                {
                    this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                    {

                        pictureBox.Source = null;

                        return null;

                    }), null);

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
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(image, UriKind.Absolute);
                            bitmap.EndInit();

                            pictureBox.Source = bitmap;

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
                                    BitmapImage bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(image, UriKind.Absolute);
                                    bitmap.EndInit();

                                    pictureBox.Source = bitmap;

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

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            NextTrack();
        }

        #region Register global keys
        private void WaitKey()
        {
            while (App.Current.Dispatcher.Thread.IsAlive)
            {

                short res1 = GetAsyncKeyState(VK_MEDIA_PLAY_PAUSE);
                short res2 = GetAsyncKeyState(VK_MEDIA_PREV_TRACK);
                short res3 = GetAsyncKeyState(VK_MEDIA_NEXT_TRACK);

                //время в мс между нажатиями горячих кнопок
                int keyPause = 200;

                if (res1 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                    App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                    {
                        PlayPauseButton();
                        lastInput = DateTime.Now;
                    }));
                else
                    if (res2 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                        App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                        {
                            PrevTrack();
                            lastInput = DateTime.Now;
                        }));
                    else
                        if (res3 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                            App.Current.Dispatcher.Invoke(new Formms.MethodInvoker(delegate()
                            {
                                NextTrack();
                                lastInput = DateTime.Now;
                            }));
                Thread.Sleep(50);
            }
        }

        public const int VK_VOLUME_MUTE = 0xAD;
        public const int VK_VOLUME_DOWN = 0xAE;
        public const int VK_VOLUME_UP = 0xAF;
        public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        public const int VK_MEDIA_PREV_TRACK = 0xB1;
        public const int VK_MEDIA_STOP = 0xB2;
        public const int VK_MEDIA_PLAY_PAUSE = 0xB3;

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern short GetAsyncKeyState(int vkey);
        #endregion

        private void timer_Tick(object sender, EventArgs e)
        {
            if (progressBar.Value >= progressBar.Maximum - 2)
            {
                if (checkBoxRepeat.IsChecked==true)
                    player.CurruntPosition = 0;
                else
                {
                    Thread.Sleep(500);
                    NextTrack();
                }
            }
            try
            {
                if (progressBar.Value <= progressBar.Maximum)
                {
                    progressBar.Value = (int)player.CurruntPosition;
                    startTime.Content = player.CurruntPositionString;
                    tbManager.SetProgressValue((int)progressBar.Value,(int) progressBar.Maximum, Process.GetCurrentProcess().MainWindowHandle);
                }

            }
            catch
            {

            }

        }

        private void progressBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //double dblValue;
            //int point = e.X;
            //if (e.X < trackBar1.Width / 2 - 80)
            //    point -= 10;
            //else
            //    if (e.X > trackBar1.Width / 2 + 80)
            //        point += 10;
            //dblValue = (point / (double)trackBar1.Width) * trackBar1.Maximum;
            //player.CurruntPosition = Convert.ToInt32(dblValue);


            double dblValue;
            int point = (int)e.GetPosition(progressBar).X;
            dblValue = (point / (double)progressBar.Width) * progressBar.Maximum;
            player.CurruntPosition = Convert.ToInt32(dblValue);
        }

        #region Player events handled here
        void engine_StatusChanged(PLAYER_STATUS status)
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
            NextTrack();
        }

        private void OnStatusPaused()
        {
            ChangePlayPauseIconInButton(false);
            tbManager.SetProgressState(TaskbarProgressBarState.Paused);
            timer.Stop();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
        }

        private void OnStatusPlaying()
        {
            ChangePlayPauseIconInButton(true);
            tbManager.SetProgressState(TaskbarProgressBarState.Normal);
            timer.Start();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Pause;
        }

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
        }

        private void listBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!listBox.IsFocused)
                listBox.Focus();
        }

        private void SetTaskbarthumbnail()
        {
            //TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip
            //(Process.GetCurrentProcess().Handle, new System.Drawing.Rectangle(albumart.Location.X + 4,
            //albumart.Location.Y, albumart.Size.Width - 1, albumart.Size.Height - 4));
        }

        //private void SetAlbumArt()
        //{

        //    if (listBox.SelectedItem != null)
        //    {
        //        TagLib.File file = TagLib.File.Create(playlist.GetCurrentTrack().GetLocation);
        //        if (file.Tag.Pictures.Length > 0)
        //        {
        //            var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
        //            albumart.Image =
        //    Image.FromStream(new MemoryStream(bin)).GetThumbnailImage
        //            (100, 100, null, IntPtr.Zero);

        //        }
        //        else
        //        {
        //            albumart.Image = Properties.Resources.Image1;
        //        }
        //    }
        //}

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
                    if (playlist != null && playlist.Count() > 0)
                    {
                        var searchText = searchBox.Text.Trim();
                        if (searchText == "")
                        {
                            listBox.SelectedIndex = playlist.SelTrack;
                            return;
                        }
                        for (int i = 0; i < playlist.Count(); i++)
                        {
                            if (((PlayListVk)playlist).GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
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
            playlist.SelTrack = listBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked == true)
                if (cherga.Count > 0 && cherga[cherga.Count - 1] != playlist.SelTrack)
                    cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var searchText = searchBox.Text.Trim();
            if (playlist != null && playlist.Count() > 0)
                if (searchText != "" && listBox.SelectedIndex != -1)
                {
                    for (int i = listBox.SelectedIndex + 1; i < listBox.Items.Count; i++)
                    {
                        if (((PlayListVk)playlist).GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listBox.SelectedIndex = i;
                            break;
                        }
                        if (i == listBox.Items.Count)
                        {
                            listBox.SelectedIndex = playlist.SelTrack;
                            return;
                        }
                    }
                }
            searchBox.Focus();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var searchText = searchBox.Text.Trim();
            if (playlist != null && playlist.Count() > 0)
                if (searchText != "" && listBox.SelectedIndex != -1)
                {
                    for (int i = listBox.SelectedIndex - 1; i > 0; i--)
                    {
                        if (((PlayListVk)playlist).GetTrackN(i).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listBox.SelectedIndex = i;
                            break;
                        }
                        if (i == 0)
                        {
                            listBox.SelectedIndex = playlist.SelTrack;
                            return;
                        }
                    }
                }
            searchBox.Focus();
        }

        private void listBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && e.Key.ToString() != "Delete, Shift")
            {
                int selIndex = listBox.SelectedIndex;
                playlist.Remove(selIndex);
                if (selIndex <= playlist.SelTrack)
                    playlist.SelTrack--;
                listBox.Items.RemoveAt(selIndex);
                if (checkBoxShuffle.IsChecked==true)
                {
                    if (cherga.Contains(selIndex))
                        cherga.Remove(selIndex);
                }
                listBox.SelectedIndex = selIndex;
            }
            if (e.Key.ToString() == "Delete, Shift")
            {
                var res = Formms.MessageBox.Show("Видалення файлу", "Точно видалити назавжди файл?", Formms.MessageBoxButtons.YesNo);
                if (res == Formms.DialogResult.Yes)
                {
                    int selIndex = listBox.SelectedIndex;
                    if (playlist.SelTrack == selIndex)
                    {
                        player.Stop();
                    }
                    playlist.RemoveFile(selIndex);
                    listBox.Items.RemoveAt(selIndex);


                    if (playlist.SelTrack == selIndex)
                    {

                        if (checkBoxShuffle.IsChecked == true)
                        {
                            if (cherga.Contains(selIndex))
                                cherga.Remove(selIndex);
                        }
                        PlayTrack();
                    }
                    if (selIndex < playlist.SelTrack)
                        playlist.SelTrack--;
                    listBox.SelectedIndex = selIndex;
                }
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
            listBox.SelectedIndex = playlist.SelTrack;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            auth.Close();
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listBox.ScrollIntoView(listBox.SelectedItem);
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            searchBox.Clear();
        }


        private void ChangePlayPauseIconInButton(bool play)
        {
            if(play)
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
            if (((ToggleButton)sender).IsChecked == true)
                showInVk = true;
            if (((ToggleButton)sender).IsChecked == false)
                showInVk = false;
        }

        private double GetSize(string url)
        {

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "HEAD";
            HttpWebResponse resp = (HttpWebResponse)(req.GetResponse());
            double ss = ((double)resp.ContentLength)/1000000;
            req.Abort();
            return ss;
        }

        private void DownloadFile(string url,string name)
        {
            new Thread(() =>
                {
                    string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Zeus\\";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                    WebClient wb = new WebClient();
                    name.Replace("\"", "");
                    wb.DownloadFile(new Uri(url), path+name+".mp3");
                }).Start();
        }

        private void logOutButton_Click(object sender, RoutedEventArgs e)
        {
            auth.LogOut();

            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void ToggleButton_Click_1(object sender, RoutedEventArgs e)
        {
            var currTrack=((PlayListVk)playlist).GetCurrentTrackVK();
            string selTrack ="";
            if(currTrack!=null)
                selTrack = currTrack.aid;
            if (reverseButton.IsChecked == true)
            {
                ((PlayListVk)playlist).ReverseOn();
            }
            else
                ((PlayListVk)playlist).ReverseOff();
            FillListBox((PlayListVk)playlist);
            if(currTrack!=null)
            {
            ((PlayListVk)playlist).SelectTrackByAid(selTrack);
            listBox.SelectedIndex = ((PlayListVk)playlist).SelTrack;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlist != null)
            {
                var currTrack=((PlayListVk)playlist).GetCurrentTrackVK();
                string selTrack="";
            if(currTrack!=null)
            selTrack = currTrack.aid;

                if (sortCombobox.SelectedIndex == 0)
                    ((PlayListVk)playlist).SortByDate();
                else
                    if (sortCombobox.SelectedIndex == 1)
                        ((PlayListVk)playlist).SortByName();

                FillListBox((PlayListVk)playlist);
                if(currTrack!=null)
            {
                ((PlayListVk)playlist).SelectTrackByAid(selTrack);
                listBox.SelectedIndex = ((PlayListVk)playlist).SelTrack;
                }
            }
        }

        private string RemoveOtherSymbols(string inputStr)
        {
            string outputStr = "";
            for(int i=0;i<inputStr.Length;i++)
            {
                if (i+4<inputStr.Length && inputStr[i] == '&' && inputStr[i + 1] == 'a' && inputStr[i + 2] == 'm' && inputStr[i + 3] == 'p' && inputStr[i + 4] == ';')
                {
                    outputStr += inputStr[i];
                    continue;
                }
                else
                if (i-4>=0 && inputStr[i - 4] == '&' && inputStr[i - 3] == 'a' && inputStr[i - 2] == 'm' && inputStr[i - 1] == 'p' && inputStr[i] == ';')
                {
                    outputStr += inputStr[i];
                    continue;
                }
                else
                if (Char.IsLetterOrDigit(inputStr[i]) || Char.IsWhiteSpace(inputStr[i]))
                    outputStr += inputStr[i];
            }
            return outputStr;
        }


    }
}

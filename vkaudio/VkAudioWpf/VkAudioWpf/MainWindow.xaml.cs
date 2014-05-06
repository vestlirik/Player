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
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.WindowsAPICodePack.Taskbar;
using Formms=System.Windows.Forms;
using vkAudio;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

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

            startTime.Content = startTime.Content = "";
            progressBar.Value = 0;

            //громкость
            //trackBar2.ValueChanged += trackBar2_ValueChanged;
            //trackBar2.Value = player.Volume;
            //Attach the event handler of WMPengine
            player.StatusChanged += new Player.OnStatusUpdate(engine_StatusChanged);

            //tulbar
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

            TaskbarManager.Instance.ThumbnailToolBars.AddButtons
               (Process.GetCurrentProcess().Handle, buttonPrevious, buttonPlayPause, buttonNext);
           // TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip
            //  (Process.GetCurrentProcess().MainWindowHandle, new Rectangle(albumart.Location, albumart.Size)); 
            #endregion


            //notifyIcon1.Visible = true;
            //notifyIcon1.Icon = SystemIcons.Hand;

            logInButton.Visibility = System.Windows.Visibility.Visible;
            logOutButton.Visibility = System.Windows.Visibility.Collapsed;
            usernameLabel.Visibility = System.Windows.Visibility.Collapsed;
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
                listBox.Items.Clear();
                foreach (var elm in ((PlayListVk)playlist).GetTrackListVK())
                {
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
                    ListBoxItem lstItem = new ListBoxItem();
                    lstItem.Height = 25;
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


                    System.Windows.Controls.Label lbl= new System.Windows.Controls.Label();
                    lbl.Content=elm.artist + " - " + elm.title;
                    Grid.SetColumn(lbl, 0);
                    grid.Children.Add(lbl);

                    lbl= new System.Windows.Controls.Label();
                    lbl.Content = "320" + " kbps";
                    Grid.SetColumn(lbl, 1);
                    grid.Children.Add(lbl);
                    
                    lbl= new System.Windows.Controls.Label();
                    lbl.Content = "44100" + " Hz";
                    Grid.SetColumn(lbl, 2);
                    grid.Children.Add(lbl);
                    
                    lbl= new System.Windows.Controls.Label();
                    lbl.Content = elm.DurationString;
                    Grid.SetColumn(lbl, 3);
                    grid.Children.Add(lbl);
                    
                    lbl= new System.Windows.Controls.Label();
                    lbl.Content = "12 MB";
                    Grid.SetColumn(lbl, 4);
                    grid.Children.Add(lbl);
                    
                    Button btn=new Button();
                    btn.Style=this.FindResource("roundedButton") as Style;
                    btn.Content = "Скачати";
                    Grid.SetColumn(btn, 5);
                    grid.Children.Add(btn);

                    btn=new Button();
                    btn.Style=this.FindResource("roundedButton") as Style;
                    btn.Content = "X";
                    Grid.SetColumn(btn, 6);
                    grid.Children.Add(btn);

                    lstItem.Content = grid;

                    listBox.Items.Add(lstItem);
                }

                
            }
        }

        private void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            playlist.SelTrack = listBox.SelectedIndex;
            if (checkBoxShuffle.IsChecked==true)
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

            this.headerLabel.Content = titleLabel.Content+" - Vk Audio";

            var tmpStr = titleLabel.Content.ToString();
            var str = tmpStr.Length > 64 ? tmpStr.Substring(0, 64) : tmpStr;
            //notifyIcon1.ShowBalloonTip(500, "Наступний трек", str, ToolTipIcon.Info);

            //SetTaskbarthumbnail();

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
            if (progressBar.Value == progressBar.Maximum)
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
                    tbManager.SetProgressValue((int)progressBar.Value,(int) progressBar.Maximum, Process.GetCurrentProcess().Handle);
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
            //label6.Text = "Стоп";
            timer.Stop();
            progressBar.Value = progressBar.Maximum = 0;
            //label2.Text = "";
            //label3.Text = "";
            //label4.Text = "";
            //label5.Text = "";
            //label7.Text = "";
            //label8.Text = "";
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
            }
            headerLabel.Content = "Стоп";
        }

        private void OnStatusEnded()
        {
            //label6.Text = "Стоп";
            NextTrack();
        }

        private void OnStatusPaused()
        {
            //label6.Text = "Пауза";
            tbManager.SetProgressState(TaskbarProgressBarState.Paused);
            timer.Stop();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
        }

        private void OnStatusPlaying()
        {
            //label6.Text = "Грає";
            tbManager.SetProgressState(TaskbarProgressBarState.Normal);
            timer.Start();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Pause;
        }

        private void OnStatusNotReady()
        {
            //label6.Text = "Стоп";
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







    }
}

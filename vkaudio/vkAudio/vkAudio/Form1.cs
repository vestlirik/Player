using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using Microsoft.WindowsAPICodePack.Shell;
using System.IO;

namespace vkAudio
{
    public partial class Form1 : Form
    {
        //форма авторизации
        Auth auth;

        Player player;
        PlayList playlist;

        //время последнего нажатия горячей кнопки
        DateTime lastInput;

        List<int> cherga = new List<int>();


        private ThumbnailToolBarButton buttonPlayPause;
        private ThumbnailToolBarButton buttonNext;
        private ThumbnailToolBarButton buttonPrevious;
        TaskbarManager tbManager = TaskbarManager.Instance;


        public Form1()
        {
            InitializeComponent();
            this.Text = "Стоп";
            auth = new Auth();
            player = new Player();
            this.Focus();
            lastInput = DateTime.Now;

            timer1.Stop();

            label7.Text = label8.Text = "";
            trackBar2.ValueChanged += trackBar2_ValueChanged;
            trackBar2.Value = player.Volume;
            //Attach the event handler of WMPengine
            player.StatusChanged += new Player.OnStatusUpdate(engine_StatusChanged);

            //tulbar
            buttonPlayPause = new ThumbnailToolBarButton
            (Properties.Resources.Hopstarter_Button_Button_Play, "Play");
            buttonPlayPause.Enabled = true;
            buttonPlayPause.Click +=
                new EventHandler<ThumbnailButtonClickedEventArgs>(button3_Click);

            buttonNext = new ThumbnailToolBarButton
            (Properties.Resources.Hopstarter_Button_Button_Fast_Forward, "Next");
            buttonNext.Enabled = true;
            buttonNext.Click +=
                 new EventHandler<ThumbnailButtonClickedEventArgs>(button4_Click);

            buttonPrevious = new ThumbnailToolBarButton(Properties.Resources.Custom_Icon_Design_Pretty_Office_8_Fast_backward, "Previous");
            buttonPrevious.Click +=
              new EventHandler<ThumbnailButtonClickedEventArgs>(button2_Click);

            TaskbarManager.Instance.ThumbnailToolBars.AddButtons
               (this.Handle, buttonPrevious, buttonPlayPause, buttonNext);
            TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip
              (this.Handle, new Rectangle(albumart.Location, albumart.Size));


            notifyIcon1.Visible = true;
            notifyIcon1.Icon = SystemIcons.Hand;
        }

        void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            label10.Text = trackBar2.Value.ToString();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region Регистрация горячих клавиш
            MethodInvoker mi = new MethodInvoker(WaitKey);
            mi.BeginInvoke(null, null);
            #endregion
        }

        private void GetAuth()
        {
            if (!auth.IsAuth)
            {
                ////Авторизация в вк
                auth.ShowDialog();
                try
                {
                    ////загрузка данных о профиле
                    XmlDocument x = new XmlDocument();
                    x.Load("https://api.vk.com/method/getProfiles.xml?uid=" + auth.UserId + "&access_token=" + auth.Token);
                    // Парсим
                    var el = x.GetElementsByTagName("user")[0];
                }
                catch
                {
                    MessageBox.Show("Неможливо з'єднатися з vk.com");
                }

                //label3.Text += el.ChildNodes[1].InnerText + " " + el.ChildNodes[2].InnerText;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            GetAuth();
            if (auth.IsAuth)
            {
                playlist = new PlayListVk();
                await Task.Factory.StartNew(() =>playlist.DownloadTracks(new string[] { auth.UserId,auth.Token }));

                listBox1.Items.Clear();
                foreach (var elm in playlist.GetTrackList())
                {
                    listBox1.Items.Add(elm);
                }
            }
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            playlist.SelTrack = listBox1.SelectedIndex;
            if (checkBox2.Checked)
                cherga.Add(playlist.SelTrack);
            PlayTrack();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PrevTrack();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PlayPauseButton();
        }

        int currInCherga=-1;
        private void PrevTrack()
        {
            if (playlist != null && playlist.Count() > 0)
            {
                if (checkBox2.Checked)
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

                    listBox1.SelectedIndex = playlist.SelTrack;
                }
                PlayTrack();
            }
        }

        private void NextTrack()
        {
            if (playlist != null && playlist.Count() > 0)
            {
                if (checkBox2.Checked)
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
                listBox1.SelectedIndex = playlist.SelTrack;


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
                    if (playlist!=null && playlist.Count()>0)
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
            }


            listBox1.SelectedIndex = playlist.SelTrack;

            if (plstType.Name == "PlayListVk")
            richTextBox1.Text = ((AudioVK)currSong).GetLirycs(auth.Token);

            player.Play();


            label2.Text = player.GetName(currSong.GetLocation);
            if (label2.Text.Trim() == "")
                label2.Text = currSong.Name;

            trackBar1.Minimum = 0;
            if (plstType.Name == "PlayListLocal")
            {
               trackBar1.Maximum = player.Duration(currSong.GetLocation);
               label4.Text = player.DurationString(currSong.GetLocation);
            }
            else if (plstType.Name == "PlayListVk")
            {
                trackBar1.Maximum = int.Parse(((AudioVK)currSong).duration);
                label4.Text = ((AudioVK)currSong).DurationString;
            }
            label7.Text = player.Bitrate(currSong.GetLocation) + " КБ/с";
            label8.Text = player.Fraquency(currSong.GetLocation) + " KHz";

            timer1.Enabled = true;

            this.Text = label2.Text;

            
                var str = label2.Text.Length > 64 ? label2.Text.Substring(0, 64) : label2.Text;
                notifyIcon1.ShowBalloonTip(500, "Наступний трек", str, ToolTipIcon.Info);

            SetTaskbarthumbnail();

        }


        private void button4_Click(object sender, EventArgs e)
        {
            NextTrack();
        }

        #region Register global keys
        private void WaitKey()
        {
            while (this.IsHandleCreated)
            {

                short res1 = GetAsyncKeyState(VK_MEDIA_PLAY_PAUSE);
                short res2 = GetAsyncKeyState(VK_MEDIA_PREV_TRACK);
                short res3 = GetAsyncKeyState(VK_MEDIA_NEXT_TRACK);
                
                //время в мс между нажатиями горячих кнопок
                int keyPause = 200;

                if (res1 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                    this.Invoke(new MethodInvoker(delegate()
                    {
                        PlayPauseButton();
                        lastInput = DateTime.Now;
                    }));
                else
                    if (res2 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                    this.Invoke(new MethodInvoker(delegate()
                    {
                        PrevTrack();
                        lastInput = DateTime.Now;
                    }));
                else
                        if (res3 != 0 && (DateTime.Now - lastInput).Milliseconds > keyPause)
                    this.Invoke(new MethodInvoker(delegate()
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


        private void timer1_Tick(object sender, EventArgs e)
        {
            if (trackBar1.Value == trackBar1.Maximum)
            {
                if (checkBox1.Checked)
                    player.CurruntPosition = 0;
                else
                {
                    Thread.Sleep(500);
                    NextTrack();
                }
            }
            try
            {
                if (trackBar1.Value <= trackBar1.Maximum)
                {
                    trackBar1.Value = (int)player.CurruntPosition;
                    label5.Text = player.CurruntPositionString;
                    tbManager.SetProgressValue(trackBar1.Value, trackBar1.Maximum);

                }
                
            }
            catch
            {
                
            }
            
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //player.CurruntPosition = trackBar1.Value;
        }


        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            double dblValue;
            dblValue = (e.X / (double)trackBar1.Width) * trackBar1.Maximum;
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
            label6.Text = "Стоп";
            timer1.Stop();
            trackBar1.Value = trackBar1.Maximum = 0;
            label2.Text = "";
            label3.Text = "";
            label4.Text = "";
            label5.Text = "";
            label7.Text = "";
            label8.Text = "";
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
            }
            this.Text = "Стоп";
        }

        private void OnStatusEnded()
        {
                label6.Text = "Стоп";
                NextTrack();
        }

        private void OnStatusPaused()
        {
            label6.Text = "Пауза";
            tbManager.SetProgressState(TaskbarProgressBarState.Paused);
            timer1.Stop();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Play;
        }

        private void OnStatusPlaying()
        {
            label6.Text = "Грає";
            tbManager.SetProgressState(TaskbarProgressBarState.Normal);
            timer1.Start();

            buttonPlayPause.Icon = Properties.Resources.Hopstarter_Button_Button_Pause;
        }

        private void OnStatusNotReady()
        {
            label6.Text = "Стоп";
        }
        #endregion

        private void button5_Click(object sender, EventArgs e)
        {
            player.Stop();
            
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            player.Volume = trackBar2.Value;
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            var res=folderBrowserDialog1.ShowDialog();
            if(res==System.Windows.Forms.DialogResult.OK)
            {
                playlist = new PlayListLocal();
                await Task.Factory.StartNew(() =>playlist.DownloadTracks(new string[]{folderBrowserDialog1.SelectedPath}));

                listBox1.Items.Clear();
                foreach (var elm in playlist.GetTrackList())
                {
                    listBox1.Items.Add(elm);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            cherga.Clear();
            currInCherga = -1;
        }

        private void trackBar2_MouseDown(object sender, MouseEventArgs e)
        {
            double dblValue;
            int z = 382;
            dblValue = ((z - e.Y) / (double)(z)) * trackBar2.Maximum;
            player.Volume = Convert.ToInt32(dblValue);
            trackBar2.Value = player.Volume;
        }

        private void trackBar2_MouseMove(object sender, MouseEventArgs e)
        {
            if (!trackBar2.Focused)
                trackBar2.Focus();
        }

        private void listBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!listBox1.Focused)
                listBox1.Focus();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }


        private void SetTaskbarthumbnail()
        {
            TaskbarManager.Instance.TabbedThumbnail.SetThumbnailClip
            (this.Handle, new Rectangle(albumart.Location.X + 4,
            albumart.Location.Y, albumart.Size.Width - 1, albumart.Size.Height - 4));
        }

        private void SetAlbumArt()
        {

            if (listBox1.SelectedItem != null)
            {
                TagLib.File file = TagLib.File.Create(playlist.GetCurrentTrack().GetLocation);
                if (file.Tag.Pictures.Length > 0)
                {
                    var bin = (byte[])(file.Tag.Pictures[0].Data.Data);
                    albumart.Image =
            Image.FromStream(new MemoryStream(bin)).GetThumbnailImage
                    (100, 100, null, IntPtr.Zero);

                }
                else
                {
                    albumart.Image =  Properties.Resources.Image1;
                }
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(label2.Text);
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            Clipboard.SetText(label2.Text);
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.KeyValue < 48 && e.KeyValue > 57) || (e.KeyValue < 65 && e.KeyValue > 90))
            {
                e.Handled = true;
                return;
            }
            if (e.KeyCode != Keys.Enter)
                if(!e.Alt && !e.Control)
            {
                if (playlist != null && playlist.Count() > 0)
                {
                    var searchText = textBox1.Text.Trim();
                    if (searchText == "")
                    {
                        listBox1.SelectedIndex = playlist.SelTrack;
                        return;
                    }
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        if (listBox1.Items[i].ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            listBox1.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                if (textBox1.Text.Trim() != "" && listBox1.SelectedIndex != -1)
                    listBox1_DoubleClick(new object(), new EventArgs());
        }

        private void label12_Click(object sender, EventArgs e)
        {
            var searchText = textBox1.Text.Trim();
            if (playlist != null && playlist.Count() > 0)
            if (searchText != "" && listBox1.SelectedIndex != -1)
            {
                for (int i = listBox1.SelectedIndex + 1; i < listBox1.Items.Count; i++)
                {
                    if (listBox1.Items[i].ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        listBox1.SelectedIndex = i;
                        break;
                    }
                    if (i == listBox1.Items.Count)
                    {
                        listBox1.SelectedIndex = playlist.SelTrack;
                        return;
                    }
                }
            }
            textBox1.Focus();
        }

        private void label13_Click(object sender, EventArgs e)
        {
            var searchText = textBox1.Text.Trim();
            if (playlist != null && playlist.Count() > 0)
            if (searchText != "" && listBox1.SelectedIndex != -1)
            {
                for (int i = listBox1.SelectedIndex - 1; i > 0; i--)
                {
                    if (listBox1.Items[i].ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        listBox1.SelectedIndex = i;
                        break;
                    }
                    if (i == 0)
                    {
                        listBox1.SelectedIndex = playlist.SelTrack;
                        return;
                    }
                }
            }
            textBox1.Focus();
        }

        private void Form1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            notifyIcon1.Visible = false;
        }


    }
}

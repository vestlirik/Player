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

        public Form1()
        {
            InitializeComponent();
            auth = new Auth();
            player = new Player();
            lastInput = DateTime.Now;

            timer1.Stop();
            timer2.Stop();

            label7.Text = label8.Text = "";
            trackBar2.Value = player.Volume;
            //Attach the event handler of WMPengine
            player.StatusChanged += new Player.OnStatusUpdate(engine_StatusChanged);
            

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

                ////загрузка данных о профиле
                XmlDocument x = new XmlDocument();
                x.Load("https://api.vk.com/method/getProfiles.xml?uid=" + auth.UserId + "&access_token=" + auth.Token);
                // Парсим
                var el = x.GetElementsByTagName("user")[0];

                //label3.Text += el.ChildNodes[1].InnerText + " " + el.ChildNodes[2].InnerText;

            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            GetAuth();
            //получаем список песен асинхронно
            //await Task.Factory.StartNew(() => playlist.GetFromVK(auth.UserId.ToString(), auth.Token));

            playlist = new PlayListVk();
            playlist.DownloadTracks(new string[] { auth.UserId,auth.Token });

            listBox1.Items.Clear();
            foreach (var elm in playlist.GetTrackList())
            {
                listBox1.Items.Add(elm);
            }

        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            playlist.SelTrack = listBox1.SelectedIndex;

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

        private void PrevTrack()
        {
            if (playlist.SelTrack == 0)
                playlist.SelTrack = playlist.Count() - 1;
            else
                playlist.SelTrack--;

            listBox1.SelectedIndex = playlist.SelTrack;

            PlayTrack();
        }

        private void NextTrack()
        {
            if (checkBox2.Checked)
            {
                playlist.SelTrack = (new Random()).Next(playlist.Count() - 1);
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
                if (playlist.Count()>0)
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

            label2.Text = currSong.Name;
            player.Play();
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
                Thread.Sleep(500);
                NextTrack();
            }
            try
            {
                trackBar1.Value = (int)player.CurruntPosition;
                label5.Text = player.CurruntPositionString;
            }
            catch
            {
                timer2.Start();
            }
            
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //player.CurruntPosition = trackBar1.Value;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            NextTrack();
            timer2.Stop();
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            double dblValue;
            dblValue = (e.X / (double)trackBar1.Width) * trackBar1.Maximum;
            player.CurruntPosition = Convert.ToInt32(dblValue);// -player.CurruntPosition;
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
        }

        private void OnStatusEnded()
        {
                label6.Text = "Стоп";
                timer2.Start();
        }

        private void OnStatusPaused()
        {
            label6.Text = "Пауза";
            timer1.Stop();

        }

        private void OnStatusPlaying()
        {
            label6.Text = "Грає";
            
            timer1.Start();

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

        private void button6_Click(object sender, EventArgs e)
        {
            var res=folderBrowserDialog1.ShowDialog();
            if(res==System.Windows.Forms.DialogResult.OK)
            {
                playlist = new PlayListLocal();
                playlist.DownloadTracks(new string[]{folderBrowserDialog1.SelectedPath});

                listBox1.Items.Clear();
                foreach (var elm in playlist.GetTrackList())
                {
                    listBox1.Items.Add(elm);
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            player.Repeat = checkBox1.Checked;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trackBar2_MouseDown(object sender, MouseEventArgs e)
        {
            double dblValue;
            int z = 382;
            dblValue = ((z - e.Y) / (double)(z)) * trackBar2.Maximum;
            player.Volume = Convert.ToInt32(dblValue);
            trackBar2.Value = player.Volume;
        }
    }
}

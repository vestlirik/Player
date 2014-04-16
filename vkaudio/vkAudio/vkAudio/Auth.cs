using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace vkAudio
{
    public partial class Auth : Form
    {
        public string Token;
        public string UserId;
        public bool IsAuth;

        public Auth()
        {
            InitializeComponent();
            IsAuth = false;
        }

        private void Auth_Load(object sender, EventArgs e)
        {
            try
            {
                webBrowser1.Navigate("https://oauth.vk.com/authorize?client_id=4308145&scope=audio,status&redirect_uri=https://oauth.vk.com/blank.html&display=page&v=5.11&response_type=token");
            }
            catch
            {
                this.Hide();
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //if (webBrowser1.Document.Body.InnerText.Contains("Эта программа не может отобразить эту веб-страницу")
            //    || webBrowser1.Document.Body.InnerText.Contains("Переход на веб-страницу отменен"))
            //    this.Hide();
            //else
            if( e.Url.ToString().IndexOf("access_token") != -1)
            {//Если в коде страницы есть acces_token, то
                Token = GetBetween(e.Url.ToString(), "access_token=", "&expires", 0);
                UserId = GetBetween(e.Url.ToString(), "user_id=", "");
                IsAuth = true;
                this.Hide();
            }
        }

        //get parth of string
        public string GetBetween(string strSource , string strStart , string strEnd, int startPos = 0)
        {
            int iPos, iEnd, lenStart = strStart.Length;
            string strResult = String.Empty;
            iPos = strSource.IndexOf(strStart);
            if (strEnd != "")
                iEnd = strSource.IndexOf(strEnd, iPos + lenStart);
            else
                iEnd = strSource.Length;
            if(iPos != -1 && iEnd != -1)
                strResult = strSource.Substring(iPos + lenStart, iEnd - (iPos + lenStart));
            return strResult;
        }
    }
}

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
            webBrowser1.Navigate("https://oauth.vk.com/authorize?client_id=4141993&scope=audio&redirect_uri=http://oauth.vk.com/blank.html&display=page&response_type=token");
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if( e.Url.ToString().IndexOf("access_token") != -1)
            {//Если в коде страницы есть acces_token, то
                Token = GetBetween(e.Url.ToString(), "access_token=", "&expires", 0);
                UserId = GetBetween(e.Url.ToString(), "user_id=", "");
                IsAuth = true;
                this.Hide();
            }
            //this.Hide();
        }

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

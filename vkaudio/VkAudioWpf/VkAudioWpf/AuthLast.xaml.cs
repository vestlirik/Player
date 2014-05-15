using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VkAudioWpf
{
    /// <summary>
    /// Interaction logic for Auth.xaml
    /// </summary>
    public partial class AuthLast : Window
    {
        public string Token;
        public string UserId;
        public bool IsAuth;

        public AuthLast()
        {
            InitializeComponent();

            var wb = new System.Windows.Forms.WebBrowser();
            wb.ScriptErrorsSuppressed = true;
            wb.Dock = System.Windows.Forms.DockStyle.Fill;
            wb.DocumentCompleted+=webBrowser1_DocumentCompleted;
            formsHost.Child = wb;

            IsAuth = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ((System.Windows.Forms.WebBrowser)(formsHost.Child)).Navigate("http://www.last.fm/api/auth/?api_key=b7d62b44095bbe482030e12b4aa33572");
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
            if (e.Url.ToString().IndexOf("token") != -1)
            {//Если в коде страницы есть acces_token, то
                System.Windows.MessageBox.Show(e.Url.ToString());
                //Token = GetBetween(e.Url.ToString(), "access_token=", "&expires", 0);
                //UserId = GetBetween(e.Url.ToString(), "user_id=", "");
                //IsAuth = true;
                //this.Hide();
            }
        }

        //get parth of string
        public string GetBetween(string strSource, string strStart, string strEnd, int startPos = 0)
        {
            int iPos, iEnd, lenStart = strStart.Length;
            string strResult = String.Empty;
            iPos = strSource.IndexOf(strStart);
            if (strEnd != "")
                iEnd = strSource.IndexOf(strEnd, iPos + lenStart);
            else
                iEnd = strSource.Length;
            if (iPos != -1 && iEnd != -1)
                strResult = strSource.Substring(iPos + lenStart, iEnd - (iPos + lenStart));
            return strResult;
        }

        public void LogOut()
        {
            string[] Cookies = System.IO.Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Cookies));

            foreach (string CookieFile in Cookies)
                try
                {
                    System.IO.File.Delete(CookieFile);
                }
                catch { }

        }

        
    }
}

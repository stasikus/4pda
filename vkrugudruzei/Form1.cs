using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using xNet.Net;
using xNet.Threading;
//using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace vkrugudruzei
{
    public partial class Form1 : Form
    {
        public delegate void UpdateTextCallback(string message);
        Random rr = new Random();
        List<string> list_id = new List<string>();
        List<string> list_id_black = new List<string>();
        int sum_id_go = 0;
        int pause1 = 0;
        int pause2 = 0;
        int sum_send = 0;

        string login = "";
        string password = "";
        string topic = "";
        string msg = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            login = textBox2.Text;
            password = textBox3.Text;
            topic = textBox4.Text;
            msg = richTextBox1.Text;

            sum_send = 0;
            sum_id_go = 0;
            pause1 = Int32.Parse(textBox5.Text) * 1000;
            pause2 = Int32.Parse(textBox1.Text) * 1000;
           
            var multi = new MultiThreading(1);
            multi.Run(go_invait);
        }

        void go_invait()
        {
            var request = new HttpRequest();
            request.UserAgent = HttpHelper.ChromeUserAgent();
            request.Cookies = new CookieDictionary();

            string get = request.Get("http://4pda.ru/forum/index.php?act=login&CODE=00&").ToString();
            string post = request.Post("http://4pda.ru/forum/index.php?act=login&CODE=01", "referer=http%253A%252F%252F4pda.ru%252F&UserName=" + System.Web.HttpUtility.UrlEncode(login) + "&PassWord=" + System.Web.HttpUtility.UrlEncode(password,Encoding.GetEncoding(1251)) + "&CookieDate=1", "application/x-www-form-urlencoded").ToString();

            if (post.Contains("<i class=\"icon-profile\">"))
            {
                for (int i = 0; i < list_id.Count; i++)
                {
                    try
                    {
                        if (!list_id_black.Contains(list_id[i]))
                        {
                            list_id_black.Add(list_id[i]);

                            sum_id_go++;
                            label7.BeginInvoke(new MethodInvoker(delegate()
                                {
                                    label7.Text = sum_id_go.ToString();
                                }));

                            get = request.Get("http://4pda.ru/forum/index.php?act=qms&mid=" + list_id[i]).ToString();
                            request.AddField("X-Requested-With", "XMLHttpRequest");
                            post = request.Post("http://4pda.ru/forum/index.php?act=qms&mid=" + list_id[i] + "&xhr=body&do=1", "action=create-thread&title=" + System.Web.HttpUtility.UrlEncode(topic) + "&message=" + System.Web.HttpUtility.UrlEncode(msg), "application/x-www-form-urlencoded").ToString();

                            if (post.Contains("<div class=\"msg-content emoticons\">"))
                            {
                                sum_send++;
                                label5.BeginInvoke(new MethodInvoker(delegate()
                                {
                                    label5.Text = sum_send.ToString();
                                }));
                                Thread.Sleep(rr.Next(pause1, pause2));
                            }
                        }
                    }
                    catch { continue; }
                }

                MessageBox.Show("Готово.");
            }
            else
            {
                MessageBox.Show("Аккаунт невалид.");
            }
        }

        private void up_label4(string mes)
        {
            label4.Text = mes;
        }

        public static List<String> SearchAndInput(string str, string start, string end)
        {
            try
            {
                Regex rq = new Regex(start.Replace("[", "\\[").Replace("]", "\\]").Replace(".", "\\.").Replace("?", "\\?"));
                Regex rq1 = new Regex(end);
                List<string> ls = new List<string>();
                int p1 = 0;
                int p2 = 0;
                while (p1 < str.Length)
                {
                    Match m = rq.Match(str, p1);
                    if (m.Success)
                    {
                        p1 = m.Index + start.ToString().Length;
                        Match m1 = rq1.Match(str, p1);
                        if (m1.Success)
                        {
                            p2 = m1.Index;
                            if (str.Substring(p1, p2 - p1) == "")
                            {
                                ls.Add(str.Substring(p1, p2 - p1));
                            }
                            else ls.Add(str.Substring(p1, p2 - p1));
                        }
                    }
                    else break;
                }
                return ls;
            }
            catch (Exception ex)
            {
                // //MessageBox.Show(ex.Message + " ||| SearchAndInput - Error");
                return null;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                list_id.Clear();
                using (StreamReader sr = new StreamReader(openFileDialog1.FileName, Encoding.GetEncoding(1251)))
                {
                    string str = sr.ReadToEnd();
                    sr.Close();
                    list_id.AddRange(Regex.Split(str, "\r\n"));
                    label4.Text = list_id.Count.ToString();
                }
            }
        }
    }
}

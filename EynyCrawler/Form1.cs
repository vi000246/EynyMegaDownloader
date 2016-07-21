using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EynyCrawler;
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;

namespace EynyCrawler
{
    /*reply會出錯*/
    public partial class Form1 : Form
    {

        string hostUri = System.Configuration.ConfigurationManager.AppSettings["hostUri"];
        string replyMsg = System.Configuration.ConfigurationManager.AppSettings["replyMsg"];
        string DebugMode = System.Configuration.ConfigurationManager.AppSettings["DebugMode"];
        string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public CookieCollection Cookies=new CookieCollection();
        Crawler crawler = new Crawler();
        ReplyHandler replyHandler = new ReplyHandler();
        FileHandler FileHandler = new FileHandler();
        Thread Crawler = null;

        public Form1()
        {
            InitializeComponent();
            textBox2.Text = System.Configuration.ConfigurationManager.AppSettings["account"];
            textBox3.Text = System.Configuration.ConfigurationManager.AppSettings["pwd"];
            textBox1.Text = path;
            // 綁定下拉選單
            Dictionary<string, string> test = new Dictionary<string, string>();
            test.Add("forum-2-1.html", "成人影片");
            test.Add("forum-205-1.html", "電影下載區");
            test.Add("forum-26-1.html", "遊戲下載區");
            test.Add("forum-1716-1.html", "電視劇下載區");
            comboBox1.DataSource = new BindingSource(test, null);
            comboBox1.DisplayMember = "Value";
            comboBox1.ValueMember = "Key";

            comboBox1.SelectedIndex = 0;

            textBox4.Text =replyMsg;
            dataGridView1.DataSource = FileHandler.data;

            //自動執行
        }

        private void button1_Click(object sender, EventArgs e)
        {

            try
            {

                FileHandler.CreateFile(textBox1.Text);
                DisableAllControl();
                int msgLength = replyHandler.Encode(textBox4.Text).Length;
                if (msgLength <= 30 && checkBox2.Checked)
                {
                    EnableAllControl();
                    throw new ArgumentException("回覆內容需大於30字元");
                }
                Crawler = new Thread(new ThreadStart(MainProgress));
                Crawler.Start();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        //按下開始鈕後要執行的主程序
        public void MainProgress() {
            try
            {
                string account = String.Empty;
                string pwd = String.Empty;
                string Type = String.Empty;
                this.Invoke((MethodInvoker)delegate
                {
                    account = textBox2.Text;
                    pwd = textBox3.Text;
                    Type = comboBox1.Text;
                    progressBar1.Minimum = 0;
                    progressBar1.Step = 1;
                });
                //如果沒勾選checkbox 取得登入後的cookie
                if (!checkBox1.Checked)
                {
                    //取得登入後的cookie
                    string loginUrl = String.Format(hostUri + "member.php?mod=logging&action=login&loginsubmit=yes&infloat=yes&lssubmit=yes&inajax=1");

                    string postData = String.Format("fastloginfield=username&username={0}&password={1}&quickforward=yes&handlekey=ls",
                       account, pwd);
                    crawler.getHTMLbyWebRequest(loginUrl, postData, ref Cookies);
                }

                //將日期寫入文字檔
                FileHandler.WriteText("日期:" + System.DateTime.Now.ToString() +
                    Environment.NewLine + Environment.NewLine, textBox1.Text);

                //取得前十頁文章列表的html 存到List<string> ArticleList
                List<string> ArticleList = GetArticleList(Cookies);

                this.Invoke((MethodInvoker)delegate
                {
                    label7.Text = "目前進度:分析文章列表的標題中...";
                });
                //取出前十頁包含mega關鍵字的文章標題和下載頁面的連結放入ResultUrl
                Dictionary<string, string> ResultUrl = new Dictionary<string, string>();
                foreach (string list in ArticleList)
                {
                    //取得有mega關鍵字的文章連結 append到ResultUrl
                    foreach (var article in crawler.FindLink(list))
                    {
                        //如果文章標題(key)不存在就加入到ResultUrl
                        if (!ResultUrl.ContainsKey(article.Key))
                            ResultUrl.Add(article.Key, article.Value);
                    }
                }

                this.Invoke((MethodInvoker)delegate
                {
                    //設定progress bar最大值
                    progressBar1.Maximum = ResultUrl.Count();
                    label8.Text = "總筆數:" + progressBar1.Maximum;
                });

                //取得文章的標題、文章連結、文章的html放入List<Article>
                List<Article> subPageHtml = GetArticleHtml(ResultUrl, Cookies);

                this.Invoke((MethodInvoker)delegate
                {
                    label7.Text = "目前進度:分析文章頁面中...";
                    //重設progressBar
                    progressBar1.Value = 0;
                });

                //將subPageHtml裡的html解析出結果 存檔進文字檔
                foreach (Article obj in subPageHtml)
                {
                    FileHandler.WriteText("檔名:" + obj.Title + Environment.NewLine, textBox1.Text);
                    FileHandler.WriteText("文章連結:" + hostUri + obj.link + Environment.NewLine, textBox1.Text);
                    //如果有登入 將result裡的結果寫入Text
                    if (!checkBox1.Checked)
                    {
                        //解析下載地址 
                        var result = crawler.GetDownloadLink(obj.html);
                        FileHandler.WriteResultToFile(result,textBox1.Text);
                    }
                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.PerformStep();
                        label7.Text = "目前進度:寫入檔案中..." + (int)((float)progressBar1.Value / progressBar1.Maximum * 100) + "%";
                    });
                }

                
                MessageBox.Show("全部檔案已完成!!");
                Application.ExitThread();
                Environment.Exit(0);
            }
            catch (Exception ex) {
                EnableAllControl();
                MessageBox.Show(ex.Message);
            }
        }

        //取得前十頁文章列表的html
        public List<string> GetArticleList(CookieCollection LoginCookies) {
            this.Invoke((MethodInvoker)delegate
            {
                label7.Text = String.Format("目前進度:取得文章列表中...(第1頁)");
            });
            List<string> ArticleHtml=new List<string>();
            string value = String.Empty;
            this.Invoke((MethodInvoker)delegate
            {
                //取得下拉選單選擇的連結
                 value = comboBox1.SelectedValue.ToString();
            });
            //取得論壇首頁的html
            string  result = crawler.getHTMLbyWebRequest(hostUri + value, "",ref Cookies);
            string HomePage =result;
            ArticleHtml.Add(HomePage);
            //取得第N頁的Url 用來找下一頁的文章
            Dictionary<string, string> JumpPage = crawler.FindPage(HomePage);
            //迴圈取得第2~N頁的html(文章列表)
                for (int i = 0; i < JumpPage.Count; i++)
                {
                    var entry = JumpPage.ElementAt(i);
                    //將html加入list
                    string responseFromServer = crawler.getHTMLbyWebRequest(hostUri + entry.Value,"", ref Cookies);
                    ArticleHtml.Add(responseFromServer);
                    this.Invoke((MethodInvoker)delegate
                    {
                        label7.Text = String.Format("目前進度:取得文章列表中...(第{0}頁)", (i + 1));
                    });
                }
            
            return ArticleHtml;
        }

        //回傳文章頁面的List<Article>物件 包含文章標題、文章連結、文章html
        public List<Article> GetArticleHtml(Dictionary<string, string> ResultUrl, CookieCollection LoginCookies)
        {
                List<Article> subPageHtml = new List<Article>();

                //List<Thread> threads = new List<Thread>();
                    for (int i = 0; i < ResultUrl.Count; i++)
                    {
                        var dic = ResultUrl.ElementAt(i);

                        //迴圈取出下載頁面的html 將文章標題 文章連結與下面頁面html存入araticle物件
                       // threads.Add(new Thread(() =>
                        //{
                        AddArticleHtml(dic, ref subPageHtml, Cookies);
                         //   Thread.Sleep(100);
                        //}));
                       // threads[i].Start();//start thread and pass it the port  
                        
                    }
                   // threads.WaitAll();
                 return subPageHtml;
        }

        public void AddArticleHtml(KeyValuePair<string, string> dic, ref List<Article> subPageHtml, CookieCollection LoginCookies)
        {
            //迴圈取出下載頁面的html 將文章標題 文章連結與下面頁面html存入araticle物件
            string uri = hostUri + dic.Value;
            string responseFromServer = crawler.getHTMLbyWebRequest(uri,"", ref Cookies);
            Article article = new Article(dic.Key,
                                            dic.Value,
                                            responseFromServer);

            //發表回覆訊息
            if (checkBox2.Checked)
                replyHandler.PostReply(textBox4.Text, dic,ref  Cookies,article.html);

            //將article放到List
            subPageHtml.Add(article);
            this.Invoke((MethodInvoker)delegate
            {
                //progressBar+1
                progressBar1.PerformStep();
                label7.Text = "目前進度:取得文章頁面中..."
                 + (int)((float)progressBar1.Value / progressBar1.Maximum * 100) + "%";
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //避免視窗關閉了，thread還在執行
            if (Crawler != null)
            {
                if (Crawler.IsAlive)
                {
                    Crawler.Abort();
                }
            }
        }
        //選擇存檔位置
        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        //控制是否登入的checkbox
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox2.Enabled = false;
                textBox3.Enabled = false;
            }
            else {
                textBox2.Enabled = true;
                textBox3.Enabled = true;
            }
        }
        //啟用/停用所有控制項
        public void EnableAllControl() {
            this.Invoke((MethodInvoker)delegate
            {
                button1.Enabled = true;
                button2.Enabled = true;
                textBox4.Enabled = true;
                comboBox1.Enabled = true;
            });
        }
        public void DisableAllControl() {
            this.Invoke((MethodInvoker)delegate
            {
                button1.Enabled = false;
                button2.Enabled = false;
                textBox4.Enabled = false;
                comboBox1.Enabled = false;
            });
        }
    }
}

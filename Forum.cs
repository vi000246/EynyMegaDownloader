using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace DiscuzHelper
{
    public class Forum
    {
        private string _url;
        /// <summary>
        /// 论坛网址
        /// </summary>
        public string Url
        {
            get
            {
                return this._url;
            }
            set
            {
                if (value[value.Length - 1] != '/')
                {
                    this._url = value + "/";
                }
                else
                {
                    this._url = value;
                }
                this._loginUrl = this.Url + "member.php?mod=logging&action=login&loginsubmit=yes&handlekey=login";
            }
        }


        private string _loginUrl;
        /// <summary>
        /// 论坛登录网址
        /// </summary>
        public string LoginUrl
        {
            get
            {
                return _loginUrl;
            }
        }

        /// <summary>
        /// 获取或者设置论坛Cookies
        /// </summary>
        public CookieContainer Cookies;
        /// <summary>
        /// 论坛发送消息后返回的正文
        /// </summary>
        public string Document;

        public bool _isLogged;
        /// <summary>
        /// 是否已经使用指定账户登录论坛
        /// </summary>
        public bool IsLogged
        {
            get
            {
                return _isLogged;
            }
        }
        /// <summary>
        /// 创建一个Forum实例
        /// </summary>
        /// <param name="url">论坛网址</param>
        public Forum(string url)
        {
            this.Url = url;
            //this._loginUrl = url +"member.php?mod=logging&action=login&loginsubmit=yes&handlekey=login";
            Cookies = new CookieContainer();
        }


        /// <summary>
        /// 获取指定板块发帖地址
        /// </summary>
        /// <param name="fid">版块fid</param>
        /// <returns></returns>
        public string GetPostUrl(string fid)
        {
            return this._url + "forum.php?mod=post&action=newthread&fid=" + fid + "&extra=&topicsubmit=yes";
        }

        /// <summary>
        /// 登录指定论坛用户
        /// </summary>
        /// <param name="user">论坛用户</param>
        /// <returns>登录成功返回true，失败返回false</returns>
        public bool Login(ForumUser user)
        {
            string result = SendDataByPost(this.LoginUrl,
                "loginfield=username&username=" + Uri.EscapeUriString(user.ID) + "&password=" + user.PassWord + "&questionid=0&answer=", ref Cookies);
            if (!result.Contains("登录失败") && Cookies.Count > 4)
            {
                _isLogged = true;
                return true;
            }
            else
            {
                _isLogged = false;
                return false;
            }
        }

        /// <summary>
        /// 获取发帖所需的formhash
        /// </summary>
        /// <returns>成功获取则formhash的值，不成功则返回string.Empty</returns>
        private string GetFormHash(string fid)
        {
            if (IsLogged == false)
            {
                throw new Exception("获取formhash失败，请先调用Login()函数登录用户");
            }
            else
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this.Url + "forum.php?mod=post&action=newthread&fid=" + fid);
                    request.Method = "GET";
                    request.Accept = "*/*";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CookieContainer = Cookies;

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream myResponseStream = response.GetResponseStream();
                    StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                    string result = myStreamReader.ReadToEnd();
                    myStreamReader.Close();
                    myResponseStream.Close();

                    Regex reg = new Regex("\"formhash\"\\s?value=\"[a-z0-9A-Z]+?\"");
                    string strForm = reg.Match(result, 0).Value.ToLower();
                    string value = strForm.Replace("formhash", "").Replace("value=", "").Replace("\"", "").Trim();
                    return value;
                }
                catch
                {
                    throw new Exception(" 发送请求失败，无法获取formhash");
                }
            }
        }

        /// <summary>
        /// 对指定版块发送新主题
        /// </summary>
        /// <param name="title">新主题标题</param>
        /// <param name="content">新主题内容</param>
        /// <param name="fid">版块fid</param>
        /// <returns>发送成功返回true，否则返回false</returns>
        public bool PostTheme(string title, string content, string fid)
        {
            if (IsLogged == false)
            {
                throw new Exception("发布主题失败，请先调用Login()函数登录用户");
            }
            else
            {
                string hash = GetFormHash(fid);
                if (hash != string.Empty)
                {
                    string postData = "formhash=" + hash + "&subject=" + title + "&message=" + content;
                    string result = SendDataByPost(this.GetPostUrl(fid), postData, ref Cookies);
                    Document = result;
                }
                return !Document.Contains("alert_error");
            }
        }

        #region 同步通过POST方式发送数据
        /// <summary>
        /// 通过POST方式发送数据
        /// </summary>
        /// <param name="Url">url</param>
        /// <param name="postDataStr">Post数据</param>
        /// <param name="cookie">Cookie容器</param>
        /// <returns></returns>
        private string SendDataByPost(string url, string postDataStr, ref CookieContainer cookie)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (cookie.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                cookie = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = cookie;
            }
            try
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postDataStr.Length;
                Stream myRequestStream = request.GetRequestStream();
                StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
                myStreamWriter.Write(postDataStr);
                myStreamWriter.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();

                return retString;
            }
            catch
            {
                throw new Exception("发出请求失败");
            }
        }
        #endregion
    }
}
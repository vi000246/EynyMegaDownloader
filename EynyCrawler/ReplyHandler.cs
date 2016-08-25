using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EynyCrawler
{
    /// <summary>
    /// 說明:
    /// 目前已可發文
    /// 只差encode
    /// 看來我的程式會utf8-encode兩次
    /// 所以伊莉上看起來才會第一次encode的值
    /// https://dotblogs.com.tw/jjnnykimo/2009/05/07/8331
    /// 改完記得把前面註解掉的部份還原
    /// 和密碼的部份
    /// </summary>
    public class ReplyHandler
    {
        //將文字訊息做url encode
        public string Encode(string text)
        {
            //Encoding en = System.Text.Encoding.GetEncoding("utf-8");
            Encoding myEncoding = Encoding.GetEncoding("utf-8");
            return System.Web.HttpUtility.UrlEncode(text, myEncoding);
        }

        //取得目前時間的UNIX值
        public string UnixTime() {
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return unixTimestamp.ToString();
        }

        //要post的網址  (thread-{tid}-1-{page}.html)
        public string replyUrl(string tid,string page){
            string url=String.Format(
                @"http://www82.eyny.com/forum.php?mod=post&action=reply&fid=2&tid={0}&extra=page%3D{1}&replysubmit=yes&infloat=yes&handlekey=fastpost&inajax=1"
                ,tid,page);
            return url;
        }
      
        //發表回覆 (message:回覆訊息，dic.value:文章連結，html:下載頁面的html,loginCookie:登入的cookie,cookies:下載頁面的cookie)
        public void PostReply(string message,KeyValuePair<string, string> dic,ref  CookieCollection loginCookie,string html)
        {
            try
            {
                /*說明 伊莉在回覆的地方會有sechash 藉由javascript post產生加密值丟到cookie*/
                string tid = String.Empty;
                string page = String.Empty;
                string formHash = String.Empty;
                string secHash = String.Empty;
                string responseFromServer = String.Empty;
                string posttime = String.Empty;
                Encoding encoding = System.Text.Encoding.Default;
                string reg = @"thread-(?<tid>\d{8})-\d{1}-(?<page>[A-Z0-9]{8}).html";
                string formHashReg = @"<input\stype=""hidden""\sname=""formhash""\svalue=""(?<formhash>[0-9A-Za-z]*)";
                string secHashReg = @"<input\sname=""sechash""\stype=""hidden""\svalue=""(?<sechach>[0-9A-Za-z]*)";
                string postTimeReg = @"<input\stype=""hidden""\sname=""posttime""\sid=""posttime""\svalue=""(?<posttime>\d{10})";
                //取得文章的編號 用來組reply url
                if (Regex.IsMatch(dic.Value, reg))
                {
                    var articleInfo = Regex.Match(dic.Value, reg);
                    tid = articleInfo.Groups["tid"].Value;
                    page = articleInfo.Groups["page"].Value;
                }

                //取得html裡的formHash和secHash 用來驗證回覆
                if (Regex.IsMatch(html, formHashReg))
                {
                    var match = Regex.Match(html, formHashReg);
                    formHash = match.Groups["formhash"].Value;
                }
                if (Regex.IsMatch(html, secHashReg))
                {
                    var match = Regex.Match(html, secHashReg);
                    secHash = match.Groups["sechach"].Value;
                }
                //取得postTime
                if (Regex.IsMatch(html, postTimeReg))
                {
                    var match = Regex.Match(html, postTimeReg);
                    posttime = match.Groups["posttime"].Value;
                }
             
                string hostUri = replyUrl(tid, page);
                string postData = String.Format(
                    "message={0}&sechash={1}&secanswer=eyny&posttime={2}&formhash={3}&subject=",
                    Encode(message), secHash, posttime, formHash);

                //發表回覆
                Crawler crawler = new Crawler();
                string result = crawler.getHTMLbyWebRequest(hostUri, postData, ref loginCookie,false);
                
                //等待30秒CD時間 避免被論壇ban
                int milliseconds = 30000;
                Thread.Sleep(milliseconds);

            }
            catch (Exception ex) {
                throw ex;
            }
        }

    }
}

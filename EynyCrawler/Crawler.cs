using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EynyCrawler
{
    public class Crawler
    {
        /// <summary>
        /// 取得網頁的html
        /// </summary>
        /// <param name="strUrl">此網址請加上queryString</param>
        /// <param name="postData">postData請先Encode過</param>
        /// <param name="Cookies">Cookie的記憶體位址</param>
        /// <returns></returns>

        public string getHTMLbyWebRequest(string strUrl, string postData, ref CookieCollection Cookies)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strUrl);
            if (Cookies.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
            }
            else
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(Cookies);            
                //cookie預設有同意瀏覽18禁的cookie
                Cookie eynycookie = new Cookie("djAX_e8d7_agree", "576");
                eynycookie.Domain = "eyny.com";
                request.CookieContainer.Add(eynycookie);
            }
            try
            {
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";
                request.ServicePoint.Expect100Continue = false;
                //組出查詢字串
                if (postData != null)
                {
                    byte[] postBytes = Encoding.ASCII.GetBytes(postData);
                    request.ContentLength = postBytes.Length;
                    using (var dataStream = request.GetRequestStream())
                    {
                        dataStream.Write(postBytes, 0, postBytes.Length);
                    }
                }
                Stream myRequestStream = request.GetRequestStream();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //將resp.cookie的值寫到ref的cookie裡
                Cookies.Add(response.Cookies);
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();

                return retString;
            }
            catch
            {
                throw new Exception("發出請求失敗");
            }
        }
        //取得標題有mega字眼的標題名稱和連結地址
        public Dictionary<string, string> FindLink(string html)
        {
            Dictionary<string, string> UrlList = new Dictionary<string, string>();
            try
            {
                string pattern = @"[<a\shref=""](?<url>thread-\d{8}-\d{1}-[A-Z0-9]{8}.html)[^>]*>(?<name>[^<]*(mega|MEGA|mu|MU|mg|MG)[^<]*)</a>";

                foreach (Match match in Regex.Matches(html, pattern,RegexOptions.Multiline))
                {
                    if (match.Success && !UrlList.ContainsKey(match.Groups["name"].Value))
                    {
                        //加入集合數組
                        UrlList.Add(match.Groups["name"].Value, match.Groups["url"].Value);

                        // hrefList.Add(m.Groups["href"].Value);
                        //nameList.Add(m.Groups["name"].Value);
                        //      this.TextBox3.Text += m.Groups["href"].Value + "|" + m.Groups["name"].Value + "\n";
                    }
                }
            }
            catch (Exception e) {
                throw e;
            }
            return UrlList;
        }

        //取得第N頁資料
        public Dictionary<string, string> FindPage(string html)
        {
            Dictionary<string, string> UrlList = new Dictionary<string, string>();
            try
            {
                string pattern = @"[<a\shref=""](?<url>forum-\d{1,4}-[A-Z0-9]*.html)[^>]*>(?<page>\d{1,2})</a>";
                int result;
                foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Multiline))
                {
                    if (match.Success && !UrlList.ContainsKey(match.Groups["page"].Value) &&Int32.TryParse(match.Groups["page"].Value,out result))
                    {
                        //加入集合數組
                        UrlList.Add(match.Groups["page"].Value, match.Groups["url"].Value);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return UrlList;
        }

        //取得子頁面的下載連結和解壓密碼 
        public ResultModel GetDownloadLink(string html)
        {
            Dictionary<string, string> UrlList = new Dictionary<string, string>();
            ResultModel Result = new ResultModel();
            Result.DownloadLink=new List<string>();
            try
            {
                string regFileSize = String.Empty;
                string regPassword = String.Empty;
                string regDownloadLink = @"(?<megaLink>https://mega(.co)?.nz/\#!?[a-zA-Z_!0-9-\#]{20,})";
                //說明: (<.*>)*? match html tag  把<br>前的字串都取出來
                regFileSize = @"(影片大小|檔案大小)[^0-9A-Za-z\u4e00-\u9fa5]*(<.*>)*?(?<filesize>[\u4e00-\u9fa50-9A-Za-z.\s-()/~，(<.*>)*?]*).*<br\s?/?>";
                regPassword = @"(?<password>【?(解壓縮碼|解壓密碼|密碼).*)<br\s?/>";
                string pattern = @"訪客無法瀏覽下載點";
                if (Regex.IsMatch(html, pattern, RegexOptions.Multiline))
                {
                    throw new ArgumentException("登入失敗");
                }
                //match檔案大小
                if (Regex.IsMatch(html, regFileSize))
                {
                    var filesize = Regex.Match(html, regFileSize);
                    Result.FileSize = ClearHtml(filesize.Groups["filesize"].Value);
                }
                else {
                    Result.FileSize = "無檔案大小或無法解析";
                }

                //match解壓密碼
                if (Regex.IsMatch(html, regPassword))
                {
                    var password = Regex.Match(html, regPassword);
                    Result.FilePassword = ClearHtml(password.Groups["password"].Value);
                }
                else {
                    Result.FilePassword = "無密碼或無法解析";
                }

                //match Mega下載連結
                foreach (Match match in Regex.Matches(html, regDownloadLink, RegexOptions.Multiline))
                {
                    if (match.Success && !Result.DownloadLink.Contains(match.Groups["megaLink"].Value))
                    {
                        //加入集合數組
                        Result.DownloadLink.Add(match.Groups["megaLink"].Value);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return Result;
        }

        //清除html
        public string ClearHtml(string text)//過濾html,js,css代碼
        {
            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            text = Regex.Replace(text, "<head[^>]*>(?:.|[\r\n])*?</head>", "");
            text = Regex.Replace(text, "<script[^>]*>(?:.|[\r\n])*?</script>", "");
            text = Regex.Replace(text, "<style[^>]*>(?:.|[\r\n])*?</style>", "");

            text = Regex.Replace(text, "(<[b|B][r|R]/*>)+|(<[p|P](.|\\n)*?>)", ""); //<br> 
            text = Regex.Replace(text, "\\&[a-zA-Z]{1,10};", "");
            text = Regex.Replace(text, "<[^>]*>", "");

            text = Regex.Replace(text, "(\\s*&[n|N][b|B][s|S][p|P];\\s*)+", ""); //&nbsp;
            text = Regex.Replace(text, "<(.|\\n)*?>", string.Empty); //其它任何標記
            text = Regex.Replace(text, "[\\s]{2,}", " "); //兩個或多個空格替換為一個

            text = Regex.Replace(text, "<font\\s*[a-zA-Z]*", " ");

            text = text.Replace("'", "''");
            text = text.Replace("\r\n", "");
            text = text.Replace("  ", "");
            text = text.Replace("\t", "");
            return text.Trim();
        }

       
        
    }
}

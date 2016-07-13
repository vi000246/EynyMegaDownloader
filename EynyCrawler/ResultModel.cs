using System;
using System.Collections.Generic;
using System.Net;

public class ResultModel
{
    public ResultModel()
	{

	}
    public string FilePassword;
    public List<string> DownloadLink;
    public string FileSize;

}
public class Article {
    public string Title;
    public string link;
    public string html;
    public Article(string Title,string link,string html) {
        this.Title = Title;
        this.link = link;
        this.html = html;
    }

}
public class response
{
    public string responseFromServer;
    public CookieCollection cookies;
    public response(string response, CookieCollection cookies)
    {
        this.responseFromServer = response;
        this.cookies = cookies;
    }

}
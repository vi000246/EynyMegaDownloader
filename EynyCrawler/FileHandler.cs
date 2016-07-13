using System;
using System.IO;

public class FileHandler
{
    //create一個文字檔
    public void CreateFile(string path)
	{
        string filepath = path + "\\Mega下載連結.txt";
        //建立文字檔
        if (!File.Exists(filepath))
        {
            var myFile = File.Create(filepath);
            myFile.Close();
            myFile.Dispose();
        }
	}

    //將文字寫入Text檔
    public void WriteText(string text,string path)
    {
        string filepath = path + "\\Mega下載連結.txt";
        using (TextWriter tw = new StreamWriter(filepath, true))
        {
            tw.WriteLine(text);
            tw.Close();
            tw.Dispose();
        }
    }

    //將結果寫進文字檔
    public void WriteResultToFile(ResultModel result,string path)
    {
        try
        {
            //寫入文字檔
            WriteText("檔案大小:" + result.FileSize + Environment.NewLine, path);
            WriteText("解壓密碼:" + result.FilePassword + Environment.NewLine, path);

            if (result.DownloadLink != null)
            {
                foreach (var link in result.DownloadLink)
                {
                    WriteText("Mega連結:" + link + Environment.NewLine, path);
                }
            }
            WriteText("===========================", path);
            System.Threading.Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

}

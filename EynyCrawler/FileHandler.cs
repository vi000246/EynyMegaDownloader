using System;
using System.IO;
using System.Data.SQLite;
using System.Data; 

public class FileHandler
{
    //command物件
    public SQLiteCommand db;
    //dataview物件
    public DataView data;

    public FileHandler() {
        var sqlite_connect = new SQLiteConnection("data source=EynyDownloadDB.s3db; Version=3;");
        //建立資料庫連線

        sqlite_connect.Open();// Open
        var sqlite_cmd = sqlite_connect.CreateCommand();//create command
        //將Command物件丟給全域變數
        db = sqlite_cmd;

        //呈現全部的資料
        DataSet dataSet = new DataSet();
        SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter("Select * from MainText", sqlite_connect);
        dataAdapter.Fill(dataSet);

        data = dataSet.Tables[0].DefaultView;
    }


    //create一個文字檔
    public void CreateFile(string path)
	{
        string filepath = path + "\\"+DateTime.Now.Date.ToString("MM-dd")+" Mega下載連結.txt";
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
    public void WriteResultToFile(Article result,string path)
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

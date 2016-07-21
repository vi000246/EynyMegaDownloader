using System;
using System.IO;
using System.Data.SQLite;
using System.Data;
using System.Linq;

public class FileHandler
{
    //command物件
    public SQLiteCommand db;
    //dataview物件
    public DataView data;
    //連線物件
    public SQLiteConnection sqlite_connect;

    //建構式
    public FileHandler() {
        sqlite_connect = new SQLiteConnection("data source=EynyDownloadDB.s3db; Version=3;");
        //建立資料庫連線

        sqlite_connect.Open();// Open
        var sqlite_cmd = sqlite_connect.CreateCommand();//create command
        //將Command物件丟給全域變數
        db = sqlite_cmd;


    }

    //搜尋資料
    public DataView GetAllData(string title, DateTime date_B, DateTime date_E)
    {
        //呈現全部的資料
        DataSet dataSet = new DataSet();
        SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(
            @"Select Date as 日期,FileName as 檔名,Link as 文章連結,Size as 檔案大小,
              Password as 解壓密碼,DownLoadLink as 下載連結  from MainText", sqlite_connect);
        dataAdapter.Fill(dataSet);

        DataView dv = new DataView(dataSet.Tables[0]);
        //注意 這裡的欄位是資料庫裡的欄位名稱
        if(!String.IsNullOrEmpty(title))
            dv.RowFilter = "檔名 like '%" + title+"%'";


        return dv;
    }

    //刪除所有資料
    public void DeleteAllData(){
        db.CommandText = @"Delete from MainText";
        db.ExecuteNonQuery();
    }

    //新增資料
    public void insertData(Article article) {

        //如果list沒值 就給空字串 如果有值 就用逗點分隔
        string MegaLink = article.DownloadLink.Count == 0 ? "" :
            article.DownloadLink.AsEnumerable().Aggregate((a, b) => a + "," + b);

        db.CommandText = @"INSERT INTO MainText (FileName,Link,Size,Password,DownLoadLink) 
            VALUES (@Title,@link,@FileSize,@FilePassword,@DownloadLink)";
        db.Parameters.Add(new SQLiteParameter("@Title", article.Title));
        db.Parameters.Add(new SQLiteParameter("@link", article.link));
        db.Parameters.Add(new SQLiteParameter("@FileSize", article.FileSize));
        db.Parameters.Add(new SQLiteParameter("@FilePassword", article.FilePassword));
        db.Parameters.Add(new SQLiteParameter("@DownloadLink", MegaLink));
        
        try
        {
            db.ExecuteNonQuery();
           // insertSQL.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }    
    }

    public void Dispose() {
        this.data.Dispose();
        this.db.Dispose();
        this.sqlite_connect.Close();
        this.sqlite_connect.Dispose();
    }

}

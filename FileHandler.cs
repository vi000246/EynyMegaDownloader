using System;

public class FileHandler
{
    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public FileHandler()
	{
	}

    public void createFile() {
        //建立文字檔
        string filepath = path + "\\Mega下載連結.txt";
        if (!File.Exists(filepath))
        {
            var myFile = File.Create(filepath);
            myFile.Close();
            myFile.Dispose();
        }
    }
}

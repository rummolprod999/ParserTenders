using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using FluentFTP;
using MySql.Data.MySqlClient;

namespace ParserTenders.ParserDir
{
    public class Parser : IParser
    {
        protected TypeArguments Arg;

        public Parser(TypeArguments a)
        {
            this.Arg = a;
        }

        public virtual void Parsing()
        {
        }

        public DataTable GetRegions()
        {
            string reg = "SELECT * FROM region";
            DataTable dt;
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(reg, connect);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                dt = ds.Tables[0];
            }

            return dt;
        }

        public virtual List<String> GetListArchLast(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchCurr(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchPrev(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchLast(string pathParse, string regionPath, string purchase)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchDaily(string pathParse, string regionPath, string purchase)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchDaily(string pathParse, string regionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public WorkWithFtp ClientFtp44_old()
        {
            WorkWithFtp ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "free", "free");
            return ftpCl;
        }

        public FtpClient ClientFtp44()
        {
            FtpClient client = new FtpClient("ftp.zakupki.gov.ru", "free", "free");
            client.Connect();
            return client;
        }

        public FtpClient ClientFtp223()
        {
            FtpClient client = new FtpClient("ftp.zakupki.gov.ru", "fz223free", "fz223free");
            client.Connect();
            return client;
        }

        public WorkWithFtp ClientFtp223_old()
        {
            WorkWithFtp ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "fz223free", "fz223free");
            return ftpCl;
        }

        public virtual void GetListFileArch(string arch, string pathParse, string region)
        {
        }

        public virtual void GetListFileArch(string arch, string pathParse, string region, int regionId)
        {
        }

        public virtual void GetListFileArch(string arch, string pathParse, string region, int regionId,
            string purchase)
        {
        }

        public virtual void Bolter(FileInfo f, string region, int regionId, TypeFile44 typefile)
        {
        }

        public virtual void Bolter(FileInfo f, string region, int regionId, TypeFile615 typefile)
        {
        }

        public virtual void Bolter(FileInfo f, string region, int regionId, TypeFile223 typefile)
        {
        }

        public virtual void Bolter(FileInfo f, string region, int regionId)
        {
        }

        public string GetArch44(string arch, string pathParse)
        {
            string file = "";
            int count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    string fileOnServer = $"{arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{arch}";
                    FtpClient ftp = ClientFtp44();
                    ftp.SetWorkingDirectory(pathParse);
                    ftp.DownloadFile(file, fileOnServer);
                    ftp.Disconnect();
                    /*using (Ftp client = new Ftp())
                    {
                        client.Connect("ftp.zakupki.gov.ru");    // or ConnectSSL for SSL
                        client.Login("free", "free");
                        client.ChangeFolder(PathParse);
                        client.Download(FileOnServer, file);

                        client.Close();
                    }*/
                    if (count > 1)
                    {
                        Log.Logger("Удалось скачать архив после попытки", count, pathParse);
                    }

                    return file;
                }
                catch (Exception e)
                {
                    if (count > 50)
                    {
                        Log.Logger($"Не удалось скачать файл после попытки {count}", arch, e);
                        return file;
                    }

                    count++;
                    Thread.Sleep(5000);
                }
            }
        }

        public string GetArch223(string arch, string pathParse)
        {
            string file = "";
            int count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    string fileOnServer = $"{arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{arch}";
                    FtpClient ftp = ClientFtp223();
                    ftp.SetWorkingDirectory(pathParse);
                    ftp.DownloadFile(file, fileOnServer);
                    ftp.Disconnect();
                    /*using (Ftp client = new Ftp())
                    {
                        client.Connect("ftp.zakupki.gov.ru");    // or ConnectSSL for SSL
                        client.Login("fz223free", "fz223free");
                        client.ChangeFolder(PathParse);
                        client.Download(FileOnServer, file);

                        client.Close();
                    }*/
                    if (count > 1)
                    {
                        Log.Logger("Удалось скачать архив после попытки", count, pathParse);
                    }

                    return file;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось скачать файл", arch, e);
                    if (count > 50)
                    {
                        return file;
                    }

                    count++;
                    Thread.Sleep(5000);
                }
            }
        }

        protected List<string> GetListFtp223(string pathParse)
        {
            List<string> archtemp = new List<string>();
            int count = 1;
            while (true)
            {
                try
                {
                    WorkWithFtp ftp = ClientFtp223_old();
                    ftp.ChangeWorkingDirectory(pathParse);
                    archtemp = ftp.ListDirectory();
                    if (count > 1)
                    {
                        Log.Logger("Удалось получить список архивов после попытки", count);
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("550 Failed to change directory"))
                    {
                        Log.Logger("Не смогли найти директорию", pathParse);
                        break;
                    }

                    if (count > 3)
                    {
                        Log.Logger($"Не смогли найти директорию после попытки {count}", pathParse, e);
                        break;
                    }

                    count++;
                    Thread.Sleep(2000);
                }
            }

            return archtemp;
        }

        protected List<string> GetListFtp44(string pathParse)
        {
            List<string> archtemp = new List<string>();
            int count = 1;
            while (true)
            {
                try
                {
                    WorkWithFtp ftp = ClientFtp44_old();
                    ftp.ChangeWorkingDirectory(pathParse);
                    archtemp = ftp.ListDirectory();
                    if (count > 1)
                    {
                        Log.Logger("Удалось получить список архивов после попытки", count);
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (count > 3)
                    {
                        Log.Logger($"Не смогли найти директорию после попытки {count}", pathParse, e);
                        break;
                    }

                    count++;
                    Thread.Sleep(2000);
                }
            }

            return archtemp;
        }

        public void CheckInn()
        {
            string cusNull = $"SELECT reg_num FROM {Program.Prefix}customer WHERE inn = ''";
            string getOrg = $"SELECT inn FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
            string getOdCus = "SELECT inn FROM od_customer WHERE regNumber = @reg_num";
            string updateCus = $"UPDATE {Program.Prefix}customer SET inn = @inn WHERE reg_num = @reg_num";
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(cusNull, connect);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                var dt = ds.Tables[0];
                foreach (DataRow row in dt.Rows)
                {
                    string regNum = (string) row["reg_num"];
                    MySqlCommand cmd = new MySqlCommand(getOrg, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@reg_num", regNum);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string orgInn = reader.GetString("inn");
                        reader.Close();
                        MySqlCommand cmd2 = new MySqlCommand(updateCus, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@reg_num", regNum);
                        cmd2.Parameters.AddWithValue("@inn", orgInn);
                        cmd2.ExecuteNonQuery();
                    }
                    else
                    {
                        reader.Close();
                        MySqlCommand cmd3 = new MySqlCommand(getOdCus, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@reg_num", regNum);
                        MySqlDataReader reader2 = cmd3.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            string cusInn = reader2.GetString("inn");
                            reader2.Close();
                            MySqlCommand cmd4 = new MySqlCommand(updateCus, connect);
                            cmd4.Prepare();
                            cmd4.Parameters.AddWithValue("@reg_num", regNum);
                            cmd4.Parameters.AddWithValue("@inn", cusInn);
                            cmd4.ExecuteNonQuery();
                        }
                        else
                        {
                            reader2.Close();
                        }
                    }
                }
            }
        }
    }
}
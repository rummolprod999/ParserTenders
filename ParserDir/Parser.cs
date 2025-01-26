#region

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using FluentFTP;
using MySql.Data.MySqlClient;

#endregion

namespace ParserTenders.ParserDir
{
    public class Parser : IParser
    {
        protected TypeArguments Arg;

        public Parser(TypeArguments a)
        {
            Arg = a;
        }

        public virtual void Parsing()
        {
        }

        public DataTable GetRegions()
        {
            var reg = "SELECT * FROM region";
            DataTable dt;
            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var adapter = new MySqlDataAdapter(reg, connect);
                var ds = new DataSet();
                adapter.Fill(ds);
                dt = ds.Tables[0];
            }

            return dt;
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

        public string GetArch44(string arch, string pathParse)
        {
            var file = "";
            var count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    var fileOnServer = $"{arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{arch}";
                    var ftp = ClientFtp44();
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
            var file = "";
            var count = 1;
            var timeout = 5000;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    var fileOnServer = $"{arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{arch}";
                    var ftp = ClientFtp223();
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
                    //Log.Logger("Не удалось скачать файл", arch, e);
                    if (count > 50)
                    {
                        return file;
                    }

                    Thread.Sleep(timeout);
                    count++;
                    timeout += 5000;
                }
            }
        }

        public virtual List<string> GetListArchLast(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchCurr(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchPrev(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchLast(string pathParse, string regionPath, string purchase)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchDaily(string pathParse, string regionPath, string purchase)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchDaily(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public WorkWithFtp ClientFtp44_old()
        {
            var ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "free",
                "VNIMANIE!_otkluchenie_FTP_s_01_01_2025_podrobnee_v_ATFF");
            return ftpCl;
        }

        public FtpClient ClientFtp44()
        {
            var client = new FtpClient("ftp.zakupki.gov.ru", "free",
                "VNIMANIE!_otkluchenie_FTP_s_01_01_2025_podrobnee_v_ATFF");
            client.Connect();
            return client;
        }

        public FtpClient ClientFtp223()
        {
            var client = new FtpClient("ftp.zakupki.gov.ru", "fz223free",
                "VNIMANIE!_otkluchenie_FTP_s_01_01_2025_podrobnee_v_ATFF");
            client.Connect();
            return client;
        }

        public WorkWithFtp ClientFtp223_old()
        {
            var ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "fz223free",
                "VNIMANIE!_otkluchenie_FTP_s_01_01_2025_podrobnee_v_ATFF");
            return ftpCl;
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

        protected List<string> GetListFtp223(string pathParse)
        {
            var archtemp = new List<string>();
            var count = 1;
            var timeout = 5000;
            while (true)
            {
                try
                {
                    var ftp = ClientFtp223_old();
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

                    if (count > 4)
                    {
                        Log.Logger($"Не смогли найти директорию после попытки {count}", pathParse, e);
                        break;
                    }

                    Thread.Sleep(timeout);
                    count++;
                    timeout += 5000;
                }
            }

            return archtemp;
        }

        protected List<(string, long)> GetListFtp223New(string pathParse)
        {
            var archtemp = new List<(string, long)>();
            var count = 1;
            var timeout = 5000;
            while (true)
            {
                try
                {
                    var ftp = ClientFtp223();
                    ftp.SetWorkingDirectory(pathParse);
                    var filelist = ftp.GetListing();
                    foreach (var ftpListItem in filelist)
                    {
                        var nameFile = ftpListItem.Name;
                        var sizeFile = ftpListItem.Size;
                        archtemp.Add((nameFile, sizeFile));
                    }

                    ftp.Disconnect();
                    if (count > 1)
                    {
                        Log.Logger("Удалось получить список архивов после попытки", count);
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Failed to change directory"))
                    {
                        Log.Logger("Не смогли найти директорию", pathParse);
                        break;
                    }

                    if (count > 4)
                    {
                        Log.Logger($"Не смогли найти директорию после попытки {count}", pathParse, e);
                        break;
                    }

                    Thread.Sleep(timeout);
                    count++;
                    timeout += 5000;
                }
            }

            return archtemp;
        }

        protected List<(string, long)> GetListFtp44New(string pathParse)
        {
            var archtemp = new List<(string, long)>();
            var count = 1;
            while (true)
            {
                try
                {
                    var ftp = ClientFtp44();
                    ftp.SetWorkingDirectory(pathParse);
                    var filelist = ftp.GetListing();
                    foreach (var ftpListItem in filelist)
                    {
                        var nameFile = ftpListItem.Name;
                        var sizeFile = ftpListItem.Size;
                        archtemp.Add((nameFile, sizeFile));
                    }

                    if (count > 1)
                    {
                        Log.Logger("Удалось получить список архивов после попытки", count);
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("Failed to change directory"))
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
            var archtemp = new List<string>();
            var count = 1;
            while (true)
            {
                try
                {
                    var ftp = ClientFtp44_old();
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
        
        protected string downloadArchive(string url)
        {
            var count = 5;
            var sleep = 2000;
            var dest = $"{Program.TempPath}{Path.DirectorySeparatorChar}array.zip";
            while (true)
            {
                try
                {
                    using (var client = new TimedWebClient())
                    {
                        client.Headers.Add("individualPerson_token", Program._token);
                        client.DownloadFile(url, dest);
                    }

                    break;
                }
                catch (Exception e)
                {
                    if (count <= 0)
                    {
                        Log.Logger($"Не удалось скачать {url}");
                        break;
                    }

                    count--;
                    Thread.Sleep(sleep);
                    sleep *= 2;
                }
            }

            return dest;
        }

        public void CheckInn()
        {
            var cusNull = $"SELECT reg_num FROM {Program.Prefix}customer WHERE inn = ''";
            var getOrg = $"SELECT inn FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
            var getOdCus = "SELECT inn FROM od_customer WHERE regNumber = @reg_num";
            var updateCus = $"UPDATE {Program.Prefix}customer SET inn = @inn WHERE reg_num = @reg_num";
            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var adapter = new MySqlDataAdapter(cusNull, connect);
                var ds = new DataSet();
                adapter.Fill(ds);
                var dt = ds.Tables[0];
                foreach (DataRow row in dt.Rows)
                {
                    var regNum = (string)row["reg_num"];
                    var cmd = new MySqlCommand(getOrg, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@reg_num", regNum);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        var orgInn = reader.GetString("inn");
                        reader.Close();
                        var cmd2 = new MySqlCommand(updateCus, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@reg_num", regNum);
                        cmd2.Parameters.AddWithValue("@inn", orgInn);
                        cmd2.ExecuteNonQuery();
                    }
                    else
                    {
                        reader.Close();
                        var cmd3 = new MySqlCommand(getOdCus, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@reg_num", regNum);
                        var reader2 = cmd3.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            var cusInn = reader2.GetString("inn");
                            reader2.Close();
                            var cmd4 = new MySqlCommand(updateCus, connect);
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
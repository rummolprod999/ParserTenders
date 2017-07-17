using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using FluentFTP;
using Limilabs.FTP.Client;
using MySql.Data.MySqlClient;

namespace ParserTenders
{
    public class Parser : IParser
    {
        protected TypeArguments arg;

        public Parser(TypeArguments a)
        {
            this.arg = a;
        }

        public virtual void Parsing()
        {
        }

        public DataTable GetRegions()
        {
            string reg = "SELECT * FROM region";
            DataTable dt;
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(reg, connect);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                dt = ds.Tables[0];
            }
            return dt;
        }

        public virtual List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchCurr(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchPrev(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchLast(string PathParse, string RegionPath, string purchase)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchDaily(string PathParse, string RegionPath, string purchase)
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

        public virtual void GetListFileArch(string Arch, string PathParse, string region)
        {
        }

        public virtual void GetListFileArch(string Arch, string PathParse, string region, int region_id)
        {
        }

        public virtual void GetListFileArch(string Arch, string PathParse, string region, int region_id,
            string purchase)
        {
        }

        public virtual void Bolter(FileInfo f, string region, int region_id, TypeFile44 typefile)
        {
        }

        public virtual void Bolter(FileInfo f, string region, int region_id, TypeFile223 typefile)
        {
        }

        public string GetArch44(string Arch, string PathParse)
        {
            string file = "";
            int count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    string FileOnServer = $"{Arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{Arch}";
                    FtpClient ftp = ClientFtp44();
                    ftp.SetWorkingDirectory(PathParse);
                    ftp.DownloadFile(file, FileOnServer);
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
                        Log.Logger("Удалось скачать архив после попытки", count);
                    }
                    return file;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось скачать файл", Arch, e);
                    if (count > 50)
                    {
                        return file;
                    }

                    count++;
                    Thread.Sleep(5000);
                }
            }
        }

        public string GetArch223(string Arch, string PathParse)
        {
            string file = "";
            int count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    string FileOnServer = $"{Arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{Arch}";
                    FtpClient ftp = ClientFtp223();
                    ftp.SetWorkingDirectory(PathParse);
                    ftp.DownloadFile(file, FileOnServer);
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
                        Log.Logger("Удалось скачать архив после попытки", count);
                    }
                    return file;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось скачать файл", Arch, e);
                    if (count > 50)
                    {
                        return file;
                    }

                    count++;
                    Thread.Sleep(5000);
                }
            }
        }

        public void CheckInn()
        {
            string cus_null = $"SELECT reg_num FROM {Program.Prefix}customer WHERE inn = ''";
            string get_org = $"SELECT inn FROM {Program.Prefix}organizer WHERE reg_num = @reg_num";
            string get_od_cus = "SELECT inn FROM od_customer WHERE regNumber = @reg_num";
            string update_cus = $"UPDATE {Program.Prefix}customer SET inn = @inn WHERE reg_num = @reg_num";
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(cus_null, connect);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                var dt = ds.Tables[0];
                foreach (DataRow row in dt.Rows)
                {
                    string reg_num = (string) row["reg_num"];
                    MySqlCommand cmd = new MySqlCommand(get_org, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@reg_num", reg_num);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string org_inn = reader.GetString("inn");
                        reader.Close();
                        MySqlCommand cmd2 = new MySqlCommand(update_cus, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@reg_num", reg_num);
                        cmd2.Parameters.AddWithValue("@inn", org_inn);
                        cmd2.ExecuteNonQuery();
                    }
                    else
                    {
                        reader.Close();
                        MySqlCommand cmd3 = new MySqlCommand(get_od_cus, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@reg_num", reg_num);
                        MySqlDataReader reader2 = cmd3.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            string cus_inn = reader2.GetString("inn");
                            reader2.Close();
                            MySqlCommand cmd4 = new MySqlCommand(update_cus, connect);
                            cmd4.Prepare();
                            cmd4.Parameters.AddWithValue("@reg_num", reg_num);
                            cmd4.Parameters.AddWithValue("@inn", cus_inn);
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
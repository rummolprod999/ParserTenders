using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TikaOnDotNet.TextExtraction;

namespace ParserTenders
{
    public class ParserAttach
    {
        protected TypeArguments arg;
        protected List<AttachStruct> ListAttach = new List<AttachStruct>();
        protected List<string> proxyList;
        protected List<string> proxyListAuth;
        protected List<string> useragentList;
        private object locker = new object();
        private object locker2 = new object();
        public event Action<int> AddAttachment;
        public event Action<int> NotAddAttachment;

        public ParserAttach(TypeArguments a)
        {
            AddAttachment += delegate(int dd)
            {
                if (dd > 0)
                    lock (locker)
                    {
                        Program.AddAttach++;
                    }
                else
                    Log.Logger("Не удалось добавить attach");
            };
            NotAddAttachment += delegate(int dd)
            {
                if (dd > 0)
                    lock (locker2)
                    {
                        Program.NotAddAttach++;
                    }
                else
                    Log.Logger("Не удалось добавить notattach");
            };
            this.arg = a;
            DataTable d = GetAttachFromDb();
            List<int> ListAttachTmp = new List<int>();
            foreach (DataRow row in d.Rows)
            {
                ListAttachTmp.Add((int) row["id_attachment"]);
            }
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                string SelectAt =
                    $"SELECT file_name, url FROM {Program.Prefix}attachment WHERE id_attachment = @id_attachment";

                foreach (var at in ListAttachTmp)
                {
                    MySqlCommand cmd = new MySqlCommand(SelectAt, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_attachment", at);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string file_name = reader.GetString("file_name");
                        string url = reader.GetString("url");
                        if (file_name.EndsWith(".doc"))
                        {
                            AttachStruct attch = new AttachStruct(at, url, TypeFileAttach.doc);
                            ListAttach.Add(attch);
                        }
                        else if (file_name.EndsWith(".docx"))
                        {
                            AttachStruct attch = new AttachStruct(at, url, TypeFileAttach.docx);
                            ListAttach.Add(attch);
                        }
                    }
                    reader.Close();
                }
            }
        }

        public void Parsing()
        {
            try
            {
                GetProxy();
            }
            catch (Exception e)
            {
                Log.Logger("Не получили список прокси", e);
                return;
            }
            try
            {
                useragentList = GetUserAgent();
            }
            catch (Exception e)
            {
                Log.Logger("Не получили список user agent", e);
                return;
            }
            try
            {
                Parallel.ForEach<AttachStruct>(ListAttach,
                    new ParallelOptions {MaxDegreeOfParallelism = Program.MaxThread}, AddAttach);
                /*foreach (var v in ListAttach)
                {
                    AddAttach(v);
                }*/
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при распараллеливании attach", e);
            }
            try
            {
                DeleteOldAttach();
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при удалении старых attach", e);
            }
        }

        public void AddAttach(AttachStruct att)
        {
            string f = "";
            try
            {
                DownloadFile d = new DownloadFile();
                f = d.DownLOld(att.url_attach, att.id_attach, att.type_f, proxyList, proxyListAuth, useragentList);
            }

            catch (Exception e)
            {
                Log.Logger("Ошибка при получении файла", e, att.url_attach);
                return;
            }
            try
            {
                string attachtext = "";
                FileInfo fileInf = new FileInfo(f);
                if (fileInf.Exists)
                {
                    try
                    {
                        var textExtractor = new TextExtractor();
                        var wordDocContents = textExtractor.Extract(f);
                        attachtext = wordDocContents.Text;
                        attachtext = Regex.Replace(attachtext, @"\s+", " ");
                        attachtext = attachtext.Trim();
                    }
                    catch (Exception e)
                    {
                        Log.Logger("Ошибка при парсинге документа", att.url_attach, e);
                    }
                    fileInf.Delete();
                }
                if (!String.IsNullOrEmpty(attachtext))
                {
                    using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                    {
                        connect.Open();
                        string UpdateA =
                            $"UPDATE {Program.Prefix}attachment SET attach_text = @attach_text, attach_add = 1 WHERE id_attachment = @id_attachment";
                        MySqlCommand cmd = new MySqlCommand(UpdateA, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@attach_text", attachtext);
                        cmd.Parameters.AddWithValue("@id_attachment", att.id_attach);
                        int addAtt = cmd.ExecuteNonQuery();
                        AddAttachment?.Invoke(addAtt);
                    }
                }
                else
                {
                    using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                    {
                        connect.Open();
                        string UpdateA =
                            $"UPDATE {Program.Prefix}attachment SET attach_add = 1 WHERE id_attachment = @id_attachment";
                        MySqlCommand cmd = new MySqlCommand(UpdateA, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_attachment", att.id_attach);
                        int addAtt = cmd.ExecuteNonQuery();
                        NotAddAttachment?.Invoke(addAtt);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге и добавлении attach", e, att.url_attach);
            }
        }

        public DataTable GetAttachFromDb()
        {
            DateTime D = Program.LocalDate.AddDays(-1);
            string DateNow = $"{D:yyyy-MM-dd 00:00:00}";
            string selectA =
                $"SELECT att.id_attachment FROM {Program.Prefix}attachment as att LEFT JOIN {Program.Prefix}tender as t ON att.id_tender = t.id_tender WHERE t.doc_publish_date >= DATE(@EndDate) AND att.attach_add = 0 AND t.cancel = 0";
            DataTable dt = new DataTable();
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                MySqlCommand cmd = new MySqlCommand(selectA, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@EndDate", DateNow);
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
            }
            return dt;
        }

        public void GetProxy()
        {
            string req =
                "http://billing.proxybox.su/api/getproxy/?format=txt&type=httpip&login=VIP182757&password=lYBdR60jRZ";
            string req_auth =
                "http://billing.proxybox.su/api/getproxy/?format=txt&type=httpauth&login=VIP182757&password=lYBdR60jRZ";

            try
            {
                List<string> p = new List<string>();
                string proxy_path =
                    $"{Program.TempPath}{Path.DirectorySeparatorChar}proxy_{Program.LocalDate:MM-dd-yyyy}.txt";
                WebClient wc = new WebClient();
                wc.DownloadFile(req, proxy_path);
                FileInfo f = new FileInfo(proxy_path);
                if (f.Exists)
                {
                    using (StreamReader sr = new StreamReader(proxy_path, System.Text.Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            p.Add(line.Trim());
                        }
                    }
                    proxyList = p;
                }
            }

            catch (Exception e)
            {
                Log.Logger("Ошибка при попытке скачать список прокси без авторизации, берем старый", e);
                string path = $"{Program.PathProgram}{Path.DirectorySeparatorChar}proxy.txt";
                List<string> p = new List<string>();
                using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        p.Add(line.Trim());
                    }
                }
                proxyList = p;
            }
            try
            {
                List<string> p = new List<string>();
                string proxy_path =
                    $"{Program.TempPath}{Path.DirectorySeparatorChar}proxy_auth_{Program.LocalDate:MM-dd-yyyy}.txt";
                WebClient wc = new WebClient();
                wc.DownloadFile(req_auth, proxy_path);
                FileInfo f = new FileInfo(proxy_path);
                if (f.Exists)
                {
                    using (StreamReader sr = new StreamReader(proxy_path, System.Text.Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            p.Add(line.Trim());
                        }
                    }
                    proxyListAuth = p;
                }
            }

            catch (Exception e)
            {
                Log.Logger("Ошибка при попытке скачать список прокси с авторизацией, берем старый", e);
                string path = $"{Program.PathProgram}{Path.DirectorySeparatorChar}proxy_auth.txt";
                List<string> p = new List<string>();
                using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        p.Add(line.Trim());
                    }
                }
                proxyListAuth = p;
            }
        }

        public List<string> GetUserAgent()
        {
            string path = "./user_agents.txt";
            List<string> p = new List<string>();
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    p.Add(line.Trim());
                }
            }
            return p;
        }

        public void DeleteOldAttach()
        {
            string DateNow = $"{Program.LocalDate:yyyy-MM-dd 00:00:00}";
            DataTable dt = new DataTable();
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                string SelectOldAttach =
                    $"SELECT att.id_attachment FROM {Program.Prefix}attachment as att LEFT JOIN {Program.Prefix}tender as t ON att.id_tender = t.id_tender WHERE t.end_date < DATE(@EndDate) AND att.attach_add = 1 AND t.cancel = 0 AND LENGTH(attach_text) > 0";
                MySqlCommand cmd = new MySqlCommand(SelectOldAttach, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@EndDate", DateNow);
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    string UpdateA =
                        $"UPDATE {Program.Prefix}attachment SET attach_text = '' WHERE id_attachment = @id_attachment";
                    MySqlCommand cmd1 = new MySqlCommand(UpdateA, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_attachment", (int) row["id_attachment"]);
                    cmd1.ExecuteNonQuery();
                }
            }
        }
    }
}
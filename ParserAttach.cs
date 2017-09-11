using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TikaOnDotNet.TextExtraction;

namespace ParserTenders
{
    public class ParserAttach
    {
        protected TypeArguments Arg;
        protected List<AttachStruct> ListAttach = new List<AttachStruct>();
        protected List<string> ProxyList;
        protected List<string> ProxyListAuth;
        protected List<string> UseragentList;
        private object _locker = new object();
        private object _locker2 = new object();
        public event Action<int> AddAttachment;
        public event Action<int> NotAddAttachment;

        public ParserAttach(TypeArguments a)
        {
            AddAttachment += delegate(int dd)
            {
                if (dd > 0)
                    lock (_locker)
                    {
                        Program.AddAttach++;
                    }
                else
                    Log.Logger("Не удалось добавить attach");
            };
            NotAddAttachment += delegate(int dd)
            {
                if (dd > 0)
                    lock (_locker2)
                    {
                        Program.NotAddAttach++;
                    }
                else
                    Log.Logger("Не удалось добавить notattach");
            };
            this.Arg = a;
            DataTable d = GetAttachFromDb();
            List<int> listAttachTmp = new List<int>();
            foreach (DataRow row in d.Rows)
            {
                listAttachTmp.Add((int) row["id_attachment"]);
            }
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                string selectAt =
                    $"SELECT file_name, url FROM {Program.Prefix}attachment WHERE id_attachment = @id_attachment";

                foreach (var at in listAttachTmp)
                {
                    MySqlCommand cmd = new MySqlCommand(selectAt, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_attachment", at);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string fileName = reader.GetString("file_name");
                        string url = reader.GetString("url");
                        if (fileName.EndsWith(".doc"))
                        {
                            AttachStruct attch = new AttachStruct(at, url, TypeFileAttach.Doc);
                            ListAttach.Add(attch);
                        }
                        else if (fileName.EndsWith(".docx"))
                        {
                            AttachStruct attch = new AttachStruct(at, url, TypeFileAttach.Docx);
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
                UseragentList = GetUserAgent();
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
                f = d.DownLOld(att.UrlAttach, att.IdAttach, att.TypeF, ProxyList, ProxyListAuth, UseragentList);
            }

            catch (Exception e)
            {
                Log.Logger("Ошибка при получении файла", e, att.UrlAttach);
                return;
            }
            try
            {
                string attachtext = "";
                FileInfo fileInf = new FileInfo(f);
                fileInf.Refresh();
                if (fileInf.Exists && fileInf.Length < 5000000)
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
                        Log.Logger("Ошибка при парсинге документа", att.UrlAttach);
                        try
                        {
                            var myProcess = new Process
                            {
                                StartInfo = new ProcessStartInfo("/opt/libreoffice5.4/program/soffice.bin",
                                    $"--headless --convert-to txt:Text {f}")
                            };
                            myProcess.Start();
                            myProcess.WaitForExit(15000);
                            if (!myProcess.HasExited)
                            {
                                myProcess.Kill();
                            }
                            string fTxt = $"{att.IdAttach}.txt";
                            FileInfo fl = new FileInfo(fTxt);
                            if (fl.Exists)
                            {
                                try
                                {
                                    using (StreamReader sr = new StreamReader(fTxt, Encoding.Default))
                                    {
                                        attachtext = sr.ReadToEnd();
                                        attachtext = Regex.Replace(attachtext, @"\s+", " ");
                                        attachtext = attachtext.Trim();
                                    }
                                    Log.Logger("Получили текст альтернативным методом", att.UrlAttach);
                                    fl.Delete();
                                }
                                catch (Exception exception)
                                {
                                    Log.Logger("Ошибка при чтении текста из файла", att.UrlAttach, exception);
                                    fl.Delete();
                                }
                            }
                            else
                            {
                                Log.Logger("Не смогли найти файл txt", fTxt);
                            }
                        }
                        catch (Exception b)
                        {
                            Log.Logger("Не Получили текст альтернативным методом", att.UrlAttach, b);
                        }
                    }
                    fileInf.Delete();
                }
                else
                {
                    if (fileInf.Exists)
                    {
                        fileInf.Delete();
                        Log.Logger("Слишком большой файл", att.UrlAttach);
                    }
                }
                if (!String.IsNullOrEmpty(attachtext))
                {
                    using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                    {
                        connect.Open();
                        string updateA =
                            $"UPDATE {Program.Prefix}attachment SET attach_text = @attach_text, attach_add = 1 WHERE id_attachment = @id_attachment";
                        MySqlCommand cmd = new MySqlCommand(updateA, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@attach_text", attachtext);
                        cmd.Parameters.AddWithValue("@id_attachment", att.IdAttach);
                        int addAtt = cmd.ExecuteNonQuery();
                        AddAttachment?.Invoke(addAtt);
                    }
                }
                else
                {
                    using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                    {
                        connect.Open();
                        string updateA =
                            $"UPDATE {Program.Prefix}attachment SET attach_add = 1 WHERE id_attachment = @id_attachment";
                        MySqlCommand cmd = new MySqlCommand(updateA, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_attachment", att.IdAttach);
                        int addAtt = cmd.ExecuteNonQuery();
                        NotAddAttachment?.Invoke(addAtt);
                    }
                    Log.Logger("Пустой текст", att.UrlAttach);
                }
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге и добавлении attach", e, att.UrlAttach);
            }
        }

        public DataTable GetAttachFromDb()
        {
            DateTime d = Program.LocalDate.AddDays(-1);
            string dateNow = $"{d:yyyy-MM-dd 00:00:00}";
            string selectA =
                $"SELECT att.id_attachment FROM {Program.Prefix}attachment as att LEFT JOIN {Program.Prefix}tender as t ON att.id_tender = t.id_tender WHERE t.doc_publish_date >= DATE(@EndDate) AND att.attach_add = 0 AND t.cancel = 0";
            DataTable dt = new DataTable();
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                MySqlCommand cmd = new MySqlCommand(selectA, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@EndDate", dateNow);
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
            }
            return dt;
        }

        public void GetProxy()
        {
            string req =
                "http://billing.proxybox.su/api/getproxy/?format=txt&type=httpip&login=VIP182757&password=lYBdR60jRZ";
            /*string req =
                "http://account.fineproxy.org/api/getproxy/?format=txt&type=httpip&login=VIP233572&password=YC2iFQFpOf";*/
            string reqAuth =
                "http://billing.proxybox.su/api/getproxy/?format=txt&type=httpauth&login=VIP182757&password=lYBdR60jRZ";
            /*string req_auth =
                "http://account.fineproxy.org/api/getproxy/?format=txt&type=httpauth&login=VIP233572&password=YC2iFQFpOf";*/

            try
            {
                List<string> p = new List<string>();
                string proxyPath =
                    $"{Program.TempPath}{Path.DirectorySeparatorChar}proxy_{Program.LocalDate:MM-dd-yyyy}.txt";
                WebClient wc = new WebClient();
                wc.DownloadFile(req, proxyPath);
                FileInfo f = new FileInfo(proxyPath);
                if (f.Exists)
                {
                    using (StreamReader sr = new StreamReader(proxyPath, System.Text.Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            p.Add(line.Trim());
                        }
                    }
                    ProxyList = p;
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
                ProxyList = p;
            }
            try
            {
                List<string> p = new List<string>();
                string proxyPath =
                    $"{Program.TempPath}{Path.DirectorySeparatorChar}proxy_auth_{Program.LocalDate:MM-dd-yyyy}.txt";
                WebClient wc = new WebClient();
                wc.DownloadFile(reqAuth, proxyPath);
                FileInfo f = new FileInfo(proxyPath);
                if (f.Exists)
                {
                    using (StreamReader sr = new StreamReader(proxyPath, System.Text.Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            p.Add(line.Trim());
                        }
                    }
                    ProxyListAuth = p;
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
                ProxyListAuth = p;
            }
        }

        public List<string> GetUserAgent()
        {
            string path = $"{Program.PathProgram}{Path.DirectorySeparatorChar}user_agents.txt";
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
            string dateNow = $"{Program.LocalDate:yyyy-MM-dd 00:00:00}";
            DataTable dt = new DataTable();
            using (MySqlConnection connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                string selectOldAttach =
                    $"SELECT att.id_attachment FROM {Program.Prefix}attachment as att LEFT JOIN {Program.Prefix}tender as t ON att.id_tender = t.id_tender WHERE t.end_date < DATE(@EndDate) AND att.attach_add = 1 AND t.cancel = 0 AND LENGTH(attach_text) > 0";
                MySqlCommand cmd = new MySqlCommand(selectOldAttach, connect);
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@EndDate", dateNow);
                MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd};
                adapter.Fill(dt);
                foreach (DataRow row in dt.Rows)
                {
                    string updateA =
                        $"UPDATE {Program.Prefix}attachment SET attach_text = '' WHERE id_attachment = @id_attachment";
                    MySqlCommand cmd1 = new MySqlCommand(updateA, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_attachment", (int) row["id_attachment"]);
                    cmd1.ExecuteNonQuery();
                }
            }
        }
    }
}
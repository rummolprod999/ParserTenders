using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ParserTenders
{
    public class ParserAttach
    {
        protected TypeArguments arg;
        protected List<AttachStruct> ListAttach = new List<AttachStruct>();
        protected List<string> proxyList;
        protected List<string> useragentList;

        public ParserAttach(TypeArguments a)
        {
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
                proxyList = GetProxy();
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
                Parallel.ForEach<AttachStruct>(ListAttach, new ParallelOptions {MaxDegreeOfParallelism = 10}, AddAttach);
                /*foreach (var v in ListAttach)
                {
                    AddAttach(v);
                }*/
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при распараллеливании attach", e);
            }
        }

        public void AddAttach(AttachStruct att)
        {
            string f = "";
            try
            {
                DownloadFile d = new DownloadFile();
                f = d.DownLOld(att.url_attach, att.id_attach, att.type_f, proxyList, useragentList);
            }
            
            catch (Exception e)
            {
                Log.Logger("Ошибка при получении файла", e);
                
            }
            FileInfo fileInf = new FileInfo(f);
            if (fileInf.Exists)
            {
                //fileInf.Delete();
                Console.WriteLine($"Скачали файл {att.url_attach}" );
            }
            else
            {
                Console.WriteLine($"Не удалось скачать файл {att.url_attach}");
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

        public List<string> GetProxy()
        {
            string path = "./proxy.txt";
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
    }
}
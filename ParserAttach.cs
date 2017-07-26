using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ParserTenders
{
    public class ParserAttach
    {
        protected TypeArguments arg;
        protected List<AttachStruct> ListAttach;

        public ParserAttach(TypeArguments a)
        {
            this.arg = a;
            DataTable d = GetAttachFromDb();
            List<int> ListAttachTmp = new List<int>();
            foreach (DataRow row in d.Rows)
            {
                ListAttachTmp.Add((int)row["id_attachment"]);
            }
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                string SelectAt = $"SELECT file_name, url FROM {Program.Prefix}attachment WHERE id_atachment = @id_atachment";
                MySqlCommand cmd = new MySqlCommand(SelectAt, connect);
                foreach (var at in ListAttachTmp)
                {
                    
                    cmd.Parameters.AddWithValue("@id_atachment", at);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        string url = 
                    }

                }
            }
            

        }

        public void Parsing()
        {
            List<AttachStruct> LAtt; 
            try
            {
                Parallel.ForEach<AttachStruct>(ListAttach, new ParallelOptions { MaxDegreeOfParallelism = 6 }, AddAttach);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при распараллеливании attach", e);
            }
        }

        public void AddAttach(AttachStruct att)
        {
            
        }

        public DataTable GetAttachFromDb()
        {
            string DateNow = $"{Program.LocalDate:yyyy-MM-dd 00:00:00}";
            string selectA =
                $"SELECT att.id_attachment FROM {Program.Prefix}attachment as att LEFT JOIN {Program.Prefix}tender as t ON att.id_tender = t.id_tender WHERE t.end_date >= DATE(@EndDate) AND att.attach_add = 0 AND t.cancel = 0";
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
    }
}
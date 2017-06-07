using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class Tender
    {
        protected readonly JObject t;
        protected readonly FileInfo file;
        protected readonly string region;
        protected readonly int region_id;
        protected readonly string file_path;

        public Tender(FileInfo f, string region, int region_id, JObject json)
        {
            t = json;
            file = f;
            this.region = region;
            this.region_id = region_id;
            file_path = file.ToString();
        }

        public virtual void Parsing()
        {
        }

        public string GetXml(string xml)
        {
            string[] xmlt = xml.Split('/');
            int t = xmlt.Length;
            if (t >= 2)
            {
                string sxml = xmlt[t - 2] + "/" + xmlt[t - 1];
                return sxml;
            }

            return "";
        }

        public List<JToken> GetElements(JToken j, string s)
        {
            List<JToken> els = new List<JToken>();
            var els_obj = j.SelectToken(s);
            if (els_obj != null && els_obj.Type != JTokenType.Null)
            {
                switch (els_obj.Type)
                {
                    case JTokenType.Object:
                        els.Add(els_obj);
                        break;
                    case JTokenType.Array:
                        els.AddRange(els_obj);
                        break;
                }
            }

            return els;
        }

        public void GetOKPD(string okpd2_code, out int okpd2_group_code, out string okpd2_group_level1_code)
        {
            if (okpd2_code.Length > 1)
            {
                int dot = okpd2_code.IndexOf(".");
                if (dot != -1)
                {
                    string okpd2_group_code_temp = okpd2_code.Substring(0, dot);
                    okpd2_group_code_temp = okpd2_group_code_temp.Substring(0, 2);
                    int temp_okpd2_group_code;
                    if (!Int32.TryParse(okpd2_group_code_temp, out temp_okpd2_group_code))
                    {
                        temp_okpd2_group_code = 0;
                    }
                    okpd2_group_code = temp_okpd2_group_code;
                }
                else
                {
                    okpd2_group_code = 0;
                }
            }
            else
            {
                okpd2_group_code = 0;
            }
            if (okpd2_code.Length > 3)
            {
                int dot = okpd2_code.IndexOf(".");
                if (dot != -1)
                {
                    okpd2_group_level1_code = okpd2_code.Substring(dot + 1, 1);
                }
                else
                {
                    okpd2_group_level1_code = "";
                }
            }
            else
            {
                okpd2_group_level1_code = "";
            }
        }

        public void TenderKwords(MySqlConnection connect, int id_tender)
        {
            string res_string = "";
            string select_pur_obj =
                $"SELECT DISTINCT po.name, po.okpd_name, cus.inn, cus.full_name FROM {Program.Prefix}customer AS cus RIGHT JOIN {Program.Prefix}purchase_object AS po ON cus.id_customer = po.id_customer LEFT JOIN {Program.Prefix}lot AS l ON l.id_lot = po.id_lot WHERE l.id_tender = @id_tender";
            MySqlCommand cmd1 = new MySqlCommand(select_pur_obj, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@id_tender", id_tender);
            DataTable dt = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                var distr_dt = dt.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (DataRow row in distr_dt)
                {
                    string name = (!row.IsNull("name")) ? ((string) row["name"]).Trim() : "";
                    string okpd_name = (!row.IsNull("okpd_name")) ? ((string) row["okpd_name"]).Trim() : "";
                    string inn_c = (!row.IsNull("inn")) ? ((string) row["inn"]).Trim() : "";
                    string full_name_c = (!row.IsNull("inn")) ? ((string) row["inn"]).Trim() : "";
                    res_string += $"{name} {okpd_name} {inn_c} {full_name_c} ";
                }
            }
            string select_attach = $"SELECT file_name FROM {Program.Prefix}attachment WHERE id_tender = @id_tender";
            MySqlCommand cmd2 = new MySqlCommand(select_attach, connect);
            cmd2.Prepare();
            cmd2.Parameters.AddWithValue("@id_tender", id_tender);
            DataTable dt2 = new DataTable();
            MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
            adapter2.Fill(dt2);
            if (dt2.Rows.Count > 0)
            {
                var distr_dt = dt2.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (DataRow row in distr_dt)
                {
                    string att_name = (!row.IsNull("file_name")) ? ((string) row["file_name"]).Trim() : "";
                    res_string += $" {att_name}";
                }
            }

            int id_org = 0;
            string select_pur_inf =
                $"SELECT purchase_object_info, id_organizer FROM {Program.Prefix}tender WHERE id_tender = @id_tender";
            MySqlCommand cmd3 = new MySqlCommand(select_pur_inf, connect);
            cmd3.Prepare();
            cmd3.Parameters.AddWithValue("@id_tender", id_tender);
            DataTable dt3 = new DataTable();
            MySqlDataAdapter adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
            adapter3.Fill(dt3);
            if (dt3.Rows.Count > 0)
            {
                foreach (DataRow row in dt3.Rows)
                {
                    string pur_ob = (!row.IsNull("purchase_object_info"))
                        ? ((string) row["purchase_object_info"]).Trim()
                        : "";
                    id_org = (!row.IsNull("id_organizer")) ? (int) row["id_organizer"] : 0;
                    res_string = $"{pur_ob} {res_string}";
                }
            }

            if (id_org != 0)
            {
                string select_org =
                    $"SELECT full_name, inn FROM {Program.Prefix}organizer WHERE id_organizer = @id_organizer";
                MySqlCommand cmd4 = new MySqlCommand(select_org, connect);
                cmd4.Prepare();
                cmd4.Parameters.AddWithValue("@id_organizer", id_org);
                DataTable dt4 = new DataTable();
                MySqlDataAdapter adapter4 = new MySqlDataAdapter {SelectCommand = cmd4};
                adapter4.Fill(dt4);
                if (dt4.Rows.Count > 0)
                {
                    foreach (DataRow row in dt4.Rows)
                    {
                        string inn_org = (!row.IsNull("inn")) ? ((string) row["inn"]).Trim() : "";
                        string name_org = (!row.IsNull("full_name")) ? ((string) row["full_name"]).Trim() : "";
                        res_string += $" {inn_org} {name_org}";
                    }
                }
            }
            res_string = Regex.Replace(res_string, @"\t|\n|\r", "");
            string update_tender =
                $"UPDATE {Program.Prefix}tender SET tender_kwords = @tender_kwords WHERE id_tender = @id_tender";
            MySqlCommand cmd5 = new MySqlCommand(update_tender, connect);
            cmd5.Prepare();
            cmd5.Parameters.AddWithValue("@id_tender", id_tender);
            cmd5.Parameters.AddWithValue("@tender_kwords", res_string);
            int res_t = cmd5.ExecuteNonQuery();
            if (res_t != 1)
            {
                Log.Logger("Не удалось обновить tender_kwords", file_path);
            }
        }
    }
}
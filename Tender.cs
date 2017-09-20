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
        protected readonly JObject T;
        protected readonly FileInfo File;
        protected readonly string Region;
        protected readonly int RegionId;
        protected readonly string FilePath;

        public Tender(FileInfo f, string region, int regionId, JObject json)
        {
            T = json;
            File = f;
            this.Region = region;
            this.RegionId = regionId;
            FilePath = File.ToString();
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
            var elsObj = j.SelectToken(s);
            if (elsObj != null && elsObj.Type != JTokenType.Null)
            {
                switch (elsObj.Type)
                {
                    case JTokenType.Object:
                        els.Add(elsObj);
                        break;
                    case JTokenType.Array:
                        els.AddRange(elsObj);
                        break;
                }
            }

            return els;
        }

        public static void GetOkpd(string okpd2Code, out int okpd2GroupCode, out string okpd2GroupLevel1Code)
        {
            if (okpd2Code.Length > 1)
            {
                int dot = okpd2Code.IndexOf(".");
                if (dot != -1)
                {
                    string okpd2GroupCodeTemp = okpd2Code.Substring(0, dot);
                    okpd2GroupCodeTemp = okpd2GroupCodeTemp.Substring(0, 2);
                    int tempOkpd2GroupCode;
                    if (!Int32.TryParse(okpd2GroupCodeTemp, out tempOkpd2GroupCode))
                    {
                        tempOkpd2GroupCode = 0;
                    }
                    okpd2GroupCode = tempOkpd2GroupCode;
                }
                else
                {
                    okpd2GroupCode = 0;
                }
            }
            else
            {
                okpd2GroupCode = 0;
            }
            if (okpd2Code.Length > 3)
            {
                int dot = okpd2Code.IndexOf(".");
                if (dot != -1)
                {
                    okpd2GroupLevel1Code = okpd2Code.Substring(dot + 1, 1);
                }
                else
                {
                    okpd2GroupLevel1Code = "";
                }
            }
            else
            {
                okpd2GroupLevel1Code = "";
            }
        }

        public static void TenderKwords(MySqlConnection connect, int idTender)
        {
            string resString = "";
            string selectPurObj =
                $"SELECT DISTINCT po.name, po.okpd_name FROM {Program.Prefix}purchase_object AS po LEFT JOIN {Program.Prefix}lot AS l ON l.id_lot = po.id_lot WHERE l.id_tender = @id_tender";
            MySqlCommand cmd1 = new MySqlCommand(selectPurObj, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@id_tender", idTender);
            DataTable dt = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                var distrDt = dt.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (DataRow row in distrDt)
                {
                    string name = !row.IsNull("name") ? ((string) row["name"]) : "";
                    string okpdName = (!row.IsNull("okpd_name")) ? ((string) row["okpd_name"]) : "";
                    resString += $"{name} {okpdName} ";
                }
            }


            string selectAttach = $"SELECT file_name FROM {Program.Prefix}attachment WHERE id_tender = @id_tender";
            MySqlCommand cmd2 = new MySqlCommand(selectAttach, connect);
            cmd2.Prepare();
            cmd2.Parameters.AddWithValue("@id_tender", idTender);
            DataTable dt2 = new DataTable();
            MySqlDataAdapter adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
            adapter2.Fill(dt2);
            if (dt2.Rows.Count > 0)
            {
                var distrDt = dt2.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (DataRow row in distrDt)
                {
                    string attName = (!row.IsNull("file_name")) ? ((string) row["file_name"]) : "";
                    resString += $" {attName}";
                }
            }

            int idOrg = 0;
            string selectPurInf =
                $"SELECT purchase_object_info, id_organizer FROM {Program.Prefix}tender WHERE id_tender = @id_tender";
            MySqlCommand cmd3 = new MySqlCommand(selectPurInf, connect);
            cmd3.Prepare();
            cmd3.Parameters.AddWithValue("@id_tender", idTender);
            DataTable dt3 = new DataTable();
            MySqlDataAdapter adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
            adapter3.Fill(dt3);
            if (dt3.Rows.Count > 0)
            {
                foreach (DataRow row in dt3.Rows)
                {
                    string purOb = (!row.IsNull("purchase_object_info"))
                        ? ((string) row["purchase_object_info"])
                        : "";
                    idOrg = (!row.IsNull("id_organizer")) ? (int) row["id_organizer"] : 0;
                    resString = $"{purOb} {resString}";
                }
            }

            if (idOrg != 0)
            {
                string selectOrg =
                    $"SELECT full_name, inn FROM {Program.Prefix}organizer WHERE id_organizer = @id_organizer";
                MySqlCommand cmd4 = new MySqlCommand(selectOrg, connect);
                cmd4.Prepare();
                cmd4.Parameters.AddWithValue("@id_organizer", idOrg);
                DataTable dt4 = new DataTable();
                MySqlDataAdapter adapter4 = new MySqlDataAdapter {SelectCommand = cmd4};
                adapter4.Fill(dt4);
                if (dt4.Rows.Count > 0)
                {
                    foreach (DataRow row in dt4.Rows)
                    {
                        string innOrg = (!row.IsNull("inn")) ? ((string) row["inn"]) : "";
                        string nameOrg = (!row.IsNull("full_name")) ? ((string) row["full_name"]) : "";
                        resString += $" {innOrg} {nameOrg}";
                    }
                }
            }

            string selectCustomer =
                $"SELECT DISTINCT cus.inn, cus.full_name FROM {Program.Prefix}customer AS cus LEFT JOIN {Program.Prefix}purchase_object AS po ON cus.id_customer = po.id_customer LEFT JOIN {Program.Prefix}lot AS l ON l.id_lot = po.id_lot WHERE l.id_tender = @id_tender";
            MySqlCommand cmd6 = new MySqlCommand(selectCustomer, connect);
            cmd6.Prepare();
            cmd6.Parameters.AddWithValue("@id_tender", idTender);
            DataTable dt5 = new DataTable();
            MySqlDataAdapter adapter5 = new MySqlDataAdapter {SelectCommand = cmd6};
            adapter5.Fill(dt5);
            if (dt5.Rows.Count > 0)
            {
                var distrDt = dt5.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (DataRow row in distrDt)
                {
                    string innC = (!row.IsNull("inn")) ? ((string) row["inn"]) : "";
                    string fullNameC = (!row.IsNull("full_name")) ? ((string) row["full_name"]) : "";
                    resString += $" {innC} {fullNameC}";
                }
            }

            resString = Regex.Replace(resString, @"\s+", " ");
            resString = resString.Trim();
            string updateTender =
                $"UPDATE {Program.Prefix}tender SET tender_kwords = @tender_kwords WHERE id_tender = @id_tender";
            MySqlCommand cmd5 = new MySqlCommand(updateTender, connect);
            cmd5.Prepare();
            cmd5.Parameters.AddWithValue("@id_tender", idTender);
            cmd5.Parameters.AddWithValue("@tender_kwords", resString);
            int resT = cmd5.ExecuteNonQuery();
            if (resT != 1)
            {
                Log.Logger("Не удалось обновить tender_kwords", idTender);
            }
        }

        public void AddVerNumber(MySqlConnection connect, string purchaseNumber)
        {
            int verNum = 1;
            string selectTenders =
                $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchaseNumber ORDER BY UNIX_TIMESTAMP(doc_publish_date) ASC";
            MySqlCommand cmd1 = new MySqlCommand(selectTenders, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@purchaseNumber", purchaseNumber);
            DataTable dt1 = new DataTable();
            MySqlDataAdapter adapter1 = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter1.Fill(dt1);
            if (dt1.Rows.Count > 0)
            {
                string updateTender =
                    $"UPDATE {Program.Prefix}tender SET num_version = @num_version WHERE id_tender = @id_tender";
                foreach (DataRow ten in dt1.Rows)
                {
                    int idTender = (int) ten["id_tender"];
                    MySqlCommand cmd2 = new MySqlCommand(updateTender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_tender", idTender);
                    cmd2.Parameters.AddWithValue("@num_version", verNum);
                    cmd2.ExecuteNonQuery();
                    verNum++;
                }
            }
        }
    }
}
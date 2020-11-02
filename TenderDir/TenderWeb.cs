using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using ClassFSAharp;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderWeb
    {
        protected readonly JObject T;
        protected readonly string FilePath;
        protected int RegionId = 0;
        protected bool isRegionExsist = default;

        public TenderWeb(JObject json, string url)
        {
            T = json;
            FilePath = url;
        }

        public virtual void Parsing()
        {
        }

        public string GetXml()
        {
            return this.FilePath;
        }

        public List<JToken> GetElements(JToken j, string s)
        {
            var els = new List<JToken>();
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
                var dot = okpd2Code.IndexOf(".");
                if (dot != -1)
                {
                    var okpd2GroupCodeTemp = okpd2Code.Substring(0, dot);
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
                var dot = okpd2Code.IndexOf(".");
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

         public static void TenderKwords(MySqlConnection connect, int idTender, bool pils = false)
        {
            var resString = "";
            if (pils)
            {
                resString = "|лекарственные средства| ";
            }

            var selectLot =
                $"SELECT DISTINCT l.lot_name FROM {Program.Prefix}tender AS t LEFT JOIN lot AS l ON l.id_tender = t.id_tender WHERE t.id_tender = @id_tender";
            var cmd0 = new MySqlCommand(selectLot, connect);
            cmd0.Prepare();
            cmd0.Parameters.AddWithValue("@id_tender", idTender);
            var dt0 = new DataTable();
            var adapter0 = new MySqlDataAdapter {SelectCommand = cmd0};
            adapter0.Fill(dt0);
            if (dt0.Rows.Count > 0)
            {
                var distrDt = dt0.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (var row in distrDt)
                {
                    var lotName = !row.IsNull("lot_name") ? ((string) row["lot_name"]) : "";
                    resString += $"{lotName} ";
                }
            }

            var selectPurObj =
                $"SELECT DISTINCT po.name, po.okpd_name FROM {Program.Prefix}purchase_object AS po LEFT JOIN {Program.Prefix}lot AS l ON l.id_lot = po.id_lot WHERE l.id_tender = @id_tender";
            var cmd1 = new MySqlCommand(selectPurObj, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@id_tender", idTender);
            var dt = new DataTable();
            var adapter = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                var distrDt = dt.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (var row in distrDt)
                {
                    var name = !row.IsNull("name") ? ((string) row["name"]) : "";
                    var okpdName = (!row.IsNull("okpd_name")) ? ((string) row["okpd_name"]) : "";
                    resString += $"{name} {okpdName} ";
                }
            }

            var selectCustReq =
                $"SELECT DISTINCT cur.delivery_term FROM {Program.Prefix}customer_requirement AS cur JOIN {Program.Prefix}lot AS l ON l.id_lot = cur.id_lot WHERE l.id_tender = @id_tender";
            var cmd7 = new MySqlCommand(selectCustReq, connect);
            cmd7.Prepare();
            cmd7.Parameters.AddWithValue("@id_tender", idTender);
            var dt7 = new DataTable();
            var adapter7 = new MySqlDataAdapter {SelectCommand = cmd7};
            adapter7.Fill(dt7);
            if (dt7.Rows.Count > 0)
            {
                var distrDeliv = dt7.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (var row in distrDeliv)
                {
                    var delivTerm = !row.IsNull("delivery_term") ? ((string) row["delivery_term"]) : "";
                    resString += $"{delivTerm} ";
                }
            }

            var selectAttach = $"SELECT file_name FROM {Program.Prefix}attachment WHERE id_tender = @id_tender";
            var cmd2 = new MySqlCommand(selectAttach, connect);
            cmd2.Prepare();
            cmd2.Parameters.AddWithValue("@id_tender", idTender);
            var dt2 = new DataTable();
            var adapter2 = new MySqlDataAdapter {SelectCommand = cmd2};
            adapter2.Fill(dt2);
            if (dt2.Rows.Count > 0)
            {
                var distrDt = dt2.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (var row in distrDt)
                {
                    var attName = (!row.IsNull("file_name")) ? ((string) row["file_name"]) : "";
                    resString += $" {attName}";
                }
            }

            var idOrg = 0;
            var selectPurInf =
                $"SELECT purchase_object_info, id_organizer FROM {Program.Prefix}tender WHERE id_tender = @id_tender";
            var cmd3 = new MySqlCommand(selectPurInf, connect);
            cmd3.Prepare();
            cmd3.Parameters.AddWithValue("@id_tender", idTender);
            var dt3 = new DataTable();
            var adapter3 = new MySqlDataAdapter {SelectCommand = cmd3};
            adapter3.Fill(dt3);
            if (dt3.Rows.Count > 0)
            {
                foreach (DataRow row in dt3.Rows)
                {
                    var purOb = (!row.IsNull("purchase_object_info"))
                        ? ((string) row["purchase_object_info"])
                        : "";
                    idOrg = (!row.IsNull("id_organizer")) ? (int) row["id_organizer"] : 0;
                    resString = $"{purOb} {resString}";
                }
            }

            var innOrg = "";
            var nameOrg = "";
            
            if (idOrg != 0)
            {
                var selectOrg =
                    $"SELECT full_name, inn FROM {Program.Prefix}organizer WHERE id_organizer = @id_organizer";
                var cmd4 = new MySqlCommand(selectOrg, connect);
                cmd4.Prepare();
                cmd4.Parameters.AddWithValue("@id_organizer", idOrg);
                var dt4 = new DataTable();
                var adapter4 = new MySqlDataAdapter {SelectCommand = cmd4};
                adapter4.Fill(dt4);
                if (dt4.Rows.Count > 0)
                {
                    foreach (DataRow row in dt4.Rows)
                    {
                        innOrg = (!row.IsNull("inn")) ? ((string) row["inn"]) : "";
                        nameOrg = (!row.IsNull("full_name")) ? ((string) row["full_name"]) : "";
                        resString += $" {innOrg} {nameOrg}";
                    }
                }
            }

            var selectCustomer =
                $"SELECT DISTINCT cus.inn, cus.full_name FROM {Program.Prefix}customer AS cus LEFT JOIN {Program.Prefix}purchase_object AS po ON cus.id_customer = po.id_customer LEFT JOIN {Program.Prefix}lot AS l ON l.id_lot = po.id_lot WHERE l.id_tender = @id_tender";
            var cmd6 = new MySqlCommand(selectCustomer, connect);
            cmd6.Prepare();
            cmd6.Parameters.AddWithValue("@id_tender", idTender);
            var dt5 = new DataTable();
            var adapter5 = new MySqlDataAdapter {SelectCommand = cmd6};
            adapter5.Fill(dt5);
            if (dt5.Rows.Count > 0)
            {
                var distrDt = dt5.AsEnumerable().Distinct(DataRowComparer.Default);
                foreach (var row in distrDt)
                {
                    var innC = (!row.IsNull("inn")) ? ((string) row["inn"]) : "";
                    var fullNameC = (!row.IsNull("full_name")) ? ((string) row["full_name"]) : "";
                    if ((innC != innOrg) || (fullNameC != nameOrg))
                    {
                        resString += $" {innC} {fullNameC}";
                    }
                }
            }

            resString = Regex.Replace(resString, @"\s+", " ");
            resString = resString.Trim();
            var updateTender =
                $"UPDATE {Program.Prefix}tender SET tender_kwords = @tender_kwords WHERE id_tender = @id_tender";
            var cmd5 = new MySqlCommand(updateTender, connect);
            cmd5.Prepare();
            cmd5.Parameters.AddWithValue("@id_tender", idTender);
            cmd5.Parameters.AddWithValue("@tender_kwords", resString);
            var resT = cmd5.ExecuteNonQuery();
            if (resT != 1)
            {
                Log.Logger("Не удалось обновить tender_kwords", idTender);
            }
        }

        public static void AddVerNumber(MySqlConnection connect, string purchaseNumber)
        {
            var verNum = 1;
            var selectTenders =
                $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchaseNumber ORDER BY UNIX_TIMESTAMP(date_version) ASC";
            var cmd1 = new MySqlCommand(selectTenders, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@purchaseNumber", purchaseNumber);
            var dt1 = new DataTable();
            var adapter1 = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter1.Fill(dt1);
            if (dt1.Rows.Count > 0)
            {
                var updateTender =
                    $"UPDATE {Program.Prefix}tender SET num_version = @num_version WHERE id_tender = @id_tender";
                foreach (DataRow ten in dt1.Rows)
                {
                    var idTender = (int) ten["id_tender"];
                    var cmd2 = new MySqlCommand(updateTender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_tender", idTender);
                    cmd2.Parameters.AddWithValue("@num_version", verNum);
                    cmd2.ExecuteNonQuery();
                    verNum++;
                }
            }
        }

        public string GetRegionString(string s)
        {
            return Tools.GetRegionString(s);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeClarification : Tender
    {
        public event Action<int> AddClarification44;

        public TenderTypeClarification(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddClarification44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddClarification++;
                else
                    Log.Logger("Не удалось добавить Clarification44", FilePath);
            };
        }

        public override void Parsing()
        {
            string xml = GetXml(File.ToString());
            JObject root = (JObject) T.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string idT = ((string) tender.SelectToken("id") ?? "").Trim();
                if (String.IsNullOrEmpty(idT))
                {
                    Log.Logger("У clarification нет id", FilePath);
                    return;
                }
                string purchaseNumber = ((string) tender.SelectToken("purchaseNumber") ?? "").Trim();
                string docNumber = ((string) tender.SelectToken("docNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У clarification нет purchaseNumber", FilePath);
                    return;
                }
                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    string selectCl =
                        $"SELECT id_clarification FROM {Program.Prefix}clarifications WHERE id_xml = @id_xml AND doc_number = @doc_number AND purchase_number = @purchase_number";
                    MySqlCommand cmd = new MySqlCommand(selectCl, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@doc_number", docNumber);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    string docPublishDate = (JsonConvert.SerializeObject(tender.SelectToken("docPublishDate") ?? "") ??
                                             "").Trim('"');
                    string href = ((string) tender.SelectToken("href") ?? "").Trim();
                    string question = ((string) tender.SelectToken("question") ?? "").Trim();
                    string topic = ((string) tender.SelectToken("topic") ?? "").Trim();
                    string insertClarification =
                        $"INSERT INTO {Program.Prefix}clarifications SET id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, doc_number = @doc_number, question = @question, topic = @topic, xml = @xml";
                    MySqlCommand cmd2 = new MySqlCommand(insertClarification, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_xml", idT);
                    cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd2.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd2.Parameters.AddWithValue("@href", href);
                    cmd2.Parameters.AddWithValue("@doc_number", docNumber);
                    cmd2.Parameters.AddWithValue("@question", question);
                    cmd2.Parameters.AddWithValue("@topic", topic);
                    cmd2.Parameters.AddWithValue("@xml", xml);
                    int resInsertC = cmd2.ExecuteNonQuery();
                    int idClar = (int) cmd2.LastInsertedId;
                    AddClarification44?.Invoke(resInsertC);
                    List<JToken> attachments = GetElements(tender, "attachments.attachment");
                    foreach (var att in attachments)
                    {
                        string attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        string attachDescription = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        string attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
                        if (!String.IsNullOrEmpty(attachName))
                        {
                            string insertAttach =
                                $"INSERT INTO {Program.Prefix}clarif_attachments SET id_clarification = @id_clarification, file_name = @file_name, url = @url, description = @description";
                            MySqlCommand cmd11 = new MySqlCommand(insertAttach, connect);
                            cmd11.Prepare();
                            cmd11.Parameters.AddWithValue("@id_clarification", idClar);
                            cmd11.Parameters.AddWithValue("@file_name", attachName);
                            cmd11.Parameters.AddWithValue("@url", attachUrl);
                            cmd11.Parameters.AddWithValue("@description", attachDescription);
                            cmd11.ExecuteNonQuery();
                        }
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Clarification", FilePath);
            }
        }
    }
}
using System;
using System.IO;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeExp223 : Tender
    {
        public event Action<int> AddExp223;

        public TenderTypeExp223(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddExp223 += delegate(int d)
            {
                if (d > 0)
                    Program.AddClarification223++;
                else
                    Log.Logger("Не удалось добавить Explanation223", FilePath);
            };
        }

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var c = (JObject) T.SelectToken("explanation.body.item.explanationData");
            if (!c.IsNullOrEmpty())
            {
                var purchaseNumber =
                    ((string) c.SelectToken("purchaseRegNum") ?? "").Trim();
                //Console.WriteLine(purchaseNumber);
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    //Log.Logger("Не могу найти purchaseNumber у sign223", FilePath);
                    return;
                }
                var idT = ((string) c.SelectToken("guid") ?? "").Trim();
                if (String.IsNullOrEmpty(idT))
                {
                    Log.Logger("У clarification нет guid", FilePath);
                    return;
                }
                var docNumber = "";
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectCl =
                        $"SELECT id_clarification FROM {Program.Prefix}clarifications WHERE id_xml = @id_xml AND purchase_number = @purchase_number";
                    var cmd = new MySqlCommand(selectCl, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    var docPublishDate = (JsonConvert.SerializeObject(c.SelectToken("publishDate") ?? "") ??
                                          "").Trim('"');
                    if (String.IsNullOrEmpty(docPublishDate))
                    {
                        docPublishDate = (JsonConvert.SerializeObject(c.SelectToken("modificationDate") ?? "") ??
                                          "").Trim('"');
                    }
                    var href = ((string) c.SelectToken("urlOOS") ?? "").Trim();
                    var question = ((string) c.SelectToken("requestSubjectInfo") ?? "").Trim();
                    var topic = ((string) c.SelectToken("description") ?? "").Trim();
                    var insertClarification =
                        $"INSERT INTO {Program.Prefix}clarifications SET id_xml = @id_xml, purchase_number = @purchase_number, doc_publish_date = @doc_publish_date, href = @href, doc_number = @doc_number, question = @question, topic = @topic, xml = @xml";
                    var cmd2 = new MySqlCommand(insertClarification, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_xml", idT);
                    cmd2.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd2.Parameters.AddWithValue("@doc_publish_date", docPublishDate);
                    cmd2.Parameters.AddWithValue("@href", href);
                    cmd2.Parameters.AddWithValue("@doc_number", docNumber);
                    cmd2.Parameters.AddWithValue("@question", question);
                    cmd2.Parameters.AddWithValue("@topic", topic);
                    cmd2.Parameters.AddWithValue("@xml", xml);
                    var resInsertC = cmd2.ExecuteNonQuery();
                    var idClar = (int) cmd2.LastInsertedId;
                    AddExp223?.Invoke(resInsertC);
                    var attachments = GetElements(c, "attachments.document");
                    foreach (var att in attachments)
                    {
                        var attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        var attachDescription = ((string) att.SelectToken("description") ?? "").Trim();
                        var attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
                        if (!String.IsNullOrEmpty(attachName))
                        {
                            var insertAttach =
                                $"INSERT INTO {Program.Prefix}clarif_attachments SET id_clarification = @id_clarification, file_name = @file_name, url = @url, description = @description";
                            var cmd11 = new MySqlCommand(insertAttach, connect);
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
                Log.Logger("Не могу найти тег explanationData", FilePath);
            }
        }
    }
}
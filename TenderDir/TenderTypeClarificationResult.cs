using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir

{
    public class TenderTypeClarificationResult: Tender
    {
        public event Action<int> AddClarification44;

        public TenderTypeClarificationResult(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddClarification44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddClarification++;
                else
                    Log.Logger("Не удалось добавить ClarificationResult", FilePath);
            };
        }

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var root = (JObject) T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("epC"));
            if (firstOrDefault != null)
            {
                var tender = firstOrDefault.Value;
                var idT = ((string) tender.SelectToken("id") ?? "").Trim();
                if (string.IsNullOrEmpty(idT))
                {
                    Log.Logger("У clarificationResult нет id", FilePath);
                    return;
                }
                var purchaseNumber = ((string) tender.SelectToken("commonInfo.purchaseNumber") ?? "").Trim();
                var docNumber = ((string) tender.SelectToken("commonInfo.docNumber") ?? "").Trim();
                if (string.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У clarificationResult нет purchaseNumber", FilePath);
                    return;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectCl =
                        $"SELECT id_clarification FROM {Program.Prefix}clarifications WHERE id_xml = @id_xml AND doc_number = @doc_number AND purchase_number = @purchase_number";
                    var cmd = new MySqlCommand(selectCl, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idT);
                    cmd.Parameters.AddWithValue("@doc_number", docNumber);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    var docPublishDate = (JsonConvert.SerializeObject(tender.SelectToken("commonInfo.docPublishDTInEIS") ?? "") ??
                                             "").Trim('"');
                    var href = ((string) tender.SelectToken("commonInfo.href") ?? "").Trim();
                    var question = ((string) tender.SelectToken("requestInfo.question") ?? "").Trim();
                    var topic = ((string) tender.SelectToken("commonInfo.topic") ?? "").Trim();
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
                    AddClarification44?.Invoke(resInsertC);
                    var attachments = GetElements(tender, "attachmentsInfo.attachmentInfo");
                    attachments.AddRange(GetElements(tender, "attachmentsInfo.attachmentInfo"));
                    foreach (var att in attachments)
                    {
                        var attachName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        var attachDescription = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        var attachUrl = ((string) att.SelectToken("url") ?? "").Trim();
                        if (!string.IsNullOrEmpty(attachName))
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
                Log.Logger("Не могу найти тег ClarificationResult", FilePath);
            }
        }
    }
}
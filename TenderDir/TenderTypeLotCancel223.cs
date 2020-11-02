using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeLotCancel223 : Tender
    {
        public event Action<int> AddLotCancel223;

        public TenderTypeLotCancel223(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddLotCancel223 += delegate(int d)
            {
                if (d > 0)
                    Program.AddLotCancel223++;
            };
        }

        public override void Parsing()
        {
            JProperty tend = null;
            var firstOrDefault = T.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            var firstOrDefault2 = ((JObject) firstOrDefault?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("body", StringComparison.Ordinal));
            var firstOrDefault3 = ((JObject) firstOrDefault2?.Value)?.Properties()
                .FirstOrDefault(p => p.Name.StartsWith("item", StringComparison.Ordinal));
            if (firstOrDefault3 != null)
            {
                tend = ((JObject) firstOrDefault3.Value).Properties()
                    .FirstOrDefault(p => p.Name.StartsWith("purchase", StringComparison.Ordinal));
            }

            if (tend != null)
            {
                var tender = tend.Value;
                var purchaseNumber = ((string) tender.SelectToken("purchaseInfo.purchaseNoticeNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("У тендера нет purchaseNumber", FilePath);
                    return;
                }

                var lots = GetElements(tender, "cancelledLots.cancelledLot");
                if (lots.Count == 0)
                {
                    Log.Logger("Can not find lots in lotcancellation", FilePath);
                    return;
                }

                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    foreach (var l in lots)
                    {
                        var lotNumber = ((string) l.SelectToken("ordinalNumber") ?? "").Trim();
                        if (String.IsNullOrEmpty(lotNumber))
                        {
                            Log.Logger("Не могу найти lotNumber у TenderLotCancel", FilePath);
                            continue;
                        }

                        var idTender = 0;
                        var selectTender =
                            $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                        var cmd = new MySqlCommand(selectTender, connect);
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@id_region", RegionId);
                        cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        var reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            reader.Read();
                            idTender = reader.GetInt32("id_tender");
                        }

                        reader.Close();
                        if (idTender == 0)
                        {
                            //Log.Logger("Не могу найти id_tender у TenderLotCancel", FilePath);
                            continue;
                        }

                        var updateTender =
                            $"UPDATE {Program.Prefix}lot SET cancel=1 WHERE id_tender = @id_tender AND lot_number = @lot_number";
                        var cmd1 = new MySqlCommand(updateTender, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@id_tender", idTender);
                        cmd1.Parameters.AddWithValue("@lot_number", lotNumber);
                        var resUpd = cmd1.ExecuteNonQuery();
                        AddLotCancel223?.Invoke(resUpd);
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderLotCancel223", FilePath);
            }
        }
    }
}
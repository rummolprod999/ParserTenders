using System;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;

namespace ParserTenders
{
    public class TenderTypeSign223 : Tender
    {
        public event Action<int> AddTenderSign223;
        public event Action<int> UpdateTenderSign223;
        
        public TenderTypeSign223(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            
            AddTenderSign223 += delegate(int d)
            {
                if (d > 0)
                    Program.AddSign223++;
                else
                    Log.Logger("Не удалось добавить TenderSign223", FilePath);
            };
            
            UpdateTenderSign223 += delegate(int d)
            {
                if (d > 0)
                    Program.UpdateSign223++;
                else
                    Log.Logger("Не удалось обновить TenderSign223", FilePath);
            };
        }

        public override void Parsing()
        {
            string xml = GetXml(File.ToString());
            JObject c = (JObject) T.SelectToken("contract.body.item.contractData");
            if (!c.IsNullOrEmpty())
            {
                string purchaseNumber = ((string) c.SelectToken("purchaseNoticeInfo.purchaseNoticeNumber") ?? "").Trim();
                //Console.WriteLine(purchaseNumber);
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у sign223", FilePath);
                    //return;
                }

                using (ContractsSignContext db = new ContractsSignContext())
                {
                    int idTender = 0;
                    MySqlParameter paramIdRegion= new MySqlParameter("@id_region", RegionId);
                    MySqlParameter paramPurchaseNumber = new MySqlParameter("@purchase_number", purchaseNumber);
                    idTender = db.Database.SqlQuery<int>($"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0", paramIdRegion, paramPurchaseNumber).FirstOrDefault();
                    //Console.WriteLine(idT);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег contractData", FilePath);
            }
        }
    }
}
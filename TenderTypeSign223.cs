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
            int Upd = 0;
            JObject c = (JObject) T.SelectToken("contract.body.item.contractData");
            if (!c.IsNullOrEmpty())
            {
                string purchaseNumber =
                    ((string) c.SelectToken("purchaseNoticeInfo.purchaseNoticeNumber") ?? "").Trim();
                //Console.WriteLine(purchaseNumber);
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у sign223", FilePath);
                    //return;
                }

                using (ContractsSignContext db = new ContractsSignContext())
                {
                    int idTender = 0;
                    MySqlParameter paramIdRegion = new MySqlParameter("@id_region", RegionId);
                    MySqlParameter paramPurchaseNumber = new MySqlParameter("@purchase_number", purchaseNumber);
                    idTender = db.Database
                        .SqlQuery<int>(
                            $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0",
                            paramIdRegion, paramPurchaseNumber).FirstOrDefault();
                    //Console.WriteLine(idT);
                    string idSign = ((string) c.SelectToken("guid") ?? "").Trim();
                    MySqlParameter paramGuid = new MySqlParameter("@id_sign", idSign);
                    MySqlParameter paramidTender = new MySqlParameter("@id_tender", idTender);
                    int idcSign = db.Database
                        .SqlQuery<int>(
                            $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign WHERE id_tender = @id_tender AND id_sign = @id_sign",
                            paramidTender, paramGuid).FirstOrDefault();
                    if (idcSign != 0)
                        return;
                    string signNumber = ((string) c.SelectToken("contractRegNumber") ?? "").Trim();
                    MySqlParameter paramSignNumber = new MySqlParameter("@sign_number", signNumber);
                    int idcSignNumber = db.Database
                        .SqlQuery<int>(
                            $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign WHERE purchase_number = @purchase_number AND sign_number = @sign_number",
                            paramPurchaseNumber, paramSignNumber).FirstOrDefault();
                    if (idcSignNumber != 0)
                        Upd = 1;
                    DateTime signDate = (DateTime?) c.SelectToken("contractDate") ?? DateTime.MinValue;
                    //Console.WriteLine(signDate);
                    string customerInn = ((string) c.SelectToken("customer.mainInfo.inn") ?? "").Trim();
                    decimal contractSignPrice = (decimal?) c.SelectToken("price") ?? 0.0m;
                    string signCurrency = ((string) c.SelectToken("currency.name") ?? "").Trim();
                    int concludeContractRight = 0;
                    DateTime protocoleDate = (DateTime?) c.SelectToken("contractConfirmingDocs.contractDoc.docDate") ??
                                             DateTime.MinValue;
                    var (supplierContact, supplierEmail, supplierContactPhone, supplierContactFax, supplierInn,
                        supplierKpp, participantType, organizationName, countryFullName, factualAddress,
                        postAddress) = ("", "", "", "", "", "", "", "", "", "", "");
                    supplierEmail = ((string) c.SelectToken("supplierInfo.address.email") ?? "").Trim();
                    supplierContactPhone = ((string) c.SelectToken("supplierInfo.address.phone") ?? "").Trim();
                    supplierContactFax = ((string) c.SelectToken("supplierInfo.address.fax") ?? "").Trim();
                    supplierInn = ((string) c.SelectToken("supplierInfo.inn") ?? "").Trim();
                    supplierKpp = ((string) c.SelectToken("supplierInfo.kpp") ?? "").Trim();
                    participantType = ((string) c.SelectToken("supplierInfo.type") ?? "").Trim();
                    organizationName = ((string) c.SelectToken("supplierInfo.name") ?? "").Trim();
                    countryFullName = ((string) c.SelectToken("supplierInfo.address.country.name") ?? "").Trim();
                    string regSup = ((string) c.SelectToken("supplierInfo.address.region.name") ?? "").Trim();
                    string citySup = ((string) c.SelectToken("supplierInfo.address.city") ?? "").Trim();
                    string streetSup = ((string) c.SelectToken("supplierInfo.address.street") ?? "").Trim();
                    factualAddress = $"{regSup} {citySup} {streetSup}".Trim();
                    int idCustomer = 0;
                    if (!String.IsNullOrEmpty(customerInn))
                    {
                        MySqlParameter paramcustomerInn = new MySqlParameter("@inn", customerInn);
                        idTender = db.Database
                            .SqlQuery<int>($"SELECT id_customer FROM {Program.Prefix}customer WHERE inn = @inn",
                                paramcustomerInn).FirstOrDefault();
                    }
                    else
                    {
                        Log.Logger("У TenderSign223 нет customer_reg_num", FilePath);
                    }
                    int idSupplier = 0;
                    Supplier sup = null;
                    if (!String.IsNullOrEmpty(supplierInn))
                    {
                        sup = db.Suppliers.FirstOrDefault(p =>
                            p.InnSupplier == supplierInn && p.KppSupplier == supplierKpp);
                        if (sup == null)
                        {
                            sup = new Supplier{ParticipiantType = participantType, InnSupplier = supplierInn, KppSupplier = supplierKpp, OrganizationName =  organizationName, CountryFullName = countryFullName, FactualAddress = factualAddress, PostAddress = postAddress, Contact = supplierContact, Email = supplierEmail, Phone = supplierContactPhone, Fax = supplierContactFax};
                            db.Suppliers.Add(sup);
                            db.SaveChanges();
                        }
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег contractData", FilePath);
            }
        }
    }
}
#region

using System;
using System.IO;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderTypeSign223New : Tender
    {
        public event Action<int> AddTenderSign223;
        public event Action<int> UpdateTenderSign223;

        public TenderTypeSign223New(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTenderSign223 += delegate(int d)
            {
                if (d > 0)
                {
                    Program.AddSign223++;
                }
                else
                {
                    Log.Logger("Не удалось добавить TenderSign223", FilePath);
                }
            };

            UpdateTenderSign223 += delegate(int d)
            {
                if (d > 0)
                {
                    Program.UpdateSign223++;
                }
                else
                {
                    Log.Logger("Не удалось обновить TenderSign223", FilePath);
                }
            };
        }

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var upd = 0;
            var c = (JObject)T.SelectToken("contract.body.item.contractData");
            if (c.IsNullOrEmpty())
            {
                return;
            }

            var purchaseNumber =
                ((string)c.SelectToken("purchaseNoticeInfo.purchaseNoticeNumber") ?? "").Trim();
            using (var connect = ConnectToDb.GetDbConnection())
            {
                var cancel = 0;
                var idTender = 0;
                connect.Open();
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
                    reader.Close();
                }
                else
                {
                    reader.Close();
                    //return;
                }

                var guid = ((string)c.SelectToken("guid") ?? "").Trim();
                var version = (int?)c.SelectToken("version") ?? 0;
                var selectSign =
                    "SELECT guid FROM purchase_contracts223 WHERE guid = @guid AND version_number = @version_number";
                var cmd1 = new MySqlCommand(selectSign, connect);
                cmd1.Prepare();
                cmd1.Parameters.AddWithValue("@guid", guid);
                cmd1.Parameters.AddWithValue("@version_number", version);
                var reader1 = cmd1.ExecuteReader();
                if (reader1.HasRows)
                {
                    reader1.Close();
                    return;
                }

                reader1.Close();
                var selectGetMax =
                    $"SELECT MAX(version_number) as m FROM {Program.Prefix}purchase_contracts223 WHERE guid = @guid";
                var cmd0 = new MySqlCommand(selectGetMax, connect);
                cmd0.Prepare();
                cmd0.Parameters.AddWithValue("@guid", guid);
                var resultm = cmd0.ExecuteScalar();
                var maxNumber = (int?)(!Convert.IsDBNull(resultm) ? resultm : null);
                if (maxNumber != null)
                {
                    if (version > maxNumber)
                    {
                        var updateC = $"UPDATE {Program.Prefix}purchase_contracts223 SET cancel=1 WHERE guid = @guid";
                        var cmd2 = new MySqlCommand(updateC, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@guid", guid);
                        cmd2.ExecuteNonQuery();
                    }
                    else
                    {
                        cancel = 1;
                    }
                }

                var idCustomer = 0;
                var idSupplier = 0;
                var customerInn = ((string)c.SelectToken("customer.mainInfo.inn") ?? "").Trim();
                var postalAddressCustomer =
                    ((string)c.SelectToken("customer.mainInfo.postalAddress") ?? "").Trim();
                if (!string.IsNullOrEmpty(customerInn))
                {
                    var selectCustomer =
                        $"SELECT id FROM {Program.Prefix}od_customer WHERE inn = @inn_customer";
                    var cmd3 = new MySqlCommand(selectCustomer, connect);
                    cmd3.Prepare();
                    cmd3.Parameters.AddWithValue("@inn_customer", customerInn);
                    var reader99 = cmd3.ExecuteReader();
                    var resRead = reader99.HasRows;
                    if (resRead)
                    {
                        reader99.Read();
                        idCustomer = reader99.GetInt32("id");
                        reader99.Close();
                    }
                    else
                    {
                        reader99.Close();
                        var kppCustomer = ((string)c.SelectToken("customer.mainInfo.kpp") ?? "").Trim();
                        var fullNameCustomer =
                            ((string)c.SelectToken("customer.mainInfo.fullName") ?? "").Trim();
                        var contractsCountCustomer = 1;
                        var contractsSumCustomer = "";
                        var contracts223CountCustomer = 0;
                        var contracts223SumCustomer = 0.0m;
                        var ogrnCustomer = ((string)c.SelectToken("customer.mainInfo.ogrn") ?? "").Trim();
                        var regionCodeCustomer = "";
                        var phoneCustomer = "";
                        var faxCustomer = "";
                        var emailCustomer = ((string)c.SelectToken("customer.mainInfo.email") ?? "").Trim();
                        var contactNameCustomer = "";
                        var addCustomer =
                            $"INSERT INTO {Program.Prefix}od_customer SET regNumber = @customer_regnumber, inn = @inn_customer, " +
                            "kpp = @kpp_customer, contracts_count = @contracts_count_customer, contracts223_count = @contracts223_count_customer," +
                            "contracts_sum = @contracts_sum_customer, contracts223_sum = @contracts223_sum_customer," +
                            "ogrn = @ogrn_customer, region_code = @region_code_customer, full_name = @full_name_customer," +
                            "postal_address = @postal_address_customer, phone = @phone_customer, fax = @fax_customer," +
                            "email = @email_customer, contact_name = @contact_name_customer";
                        var cmd4 = new MySqlCommand(addCustomer, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@customer_regnumber", "");
                        cmd4.Parameters.AddWithValue("@inn_customer", customerInn);
                        cmd4.Parameters.AddWithValue("@kpp_customer", kppCustomer);
                        cmd4.Parameters.AddWithValue("@contracts_count_customer", contractsCountCustomer);
                        cmd4.Parameters.AddWithValue("@contracts223_count_customer", contracts223CountCustomer);
                        cmd4.Parameters.AddWithValue("@contracts_sum_customer", contractsSumCustomer);
                        cmd4.Parameters.AddWithValue("@contracts223_sum_customer", contracts223SumCustomer);
                        cmd4.Parameters.AddWithValue("@ogrn_customer", ogrnCustomer);
                        cmd4.Parameters.AddWithValue("@region_code_customer", regionCodeCustomer);
                        cmd4.Parameters.AddWithValue("@full_name_customer", fullNameCustomer);
                        cmd4.Parameters.AddWithValue("@postal_address_customer", postalAddressCustomer);
                        cmd4.Parameters.AddWithValue("@phone_customer", phoneCustomer);
                        cmd4.Parameters.AddWithValue("@fax_customer", faxCustomer);
                        cmd4.Parameters.AddWithValue("@email_customer", emailCustomer);
                        cmd4.Parameters.AddWithValue("@contact_name_customer", contactNameCustomer);
                        var addC = cmd4.ExecuteNonQuery();
                        idCustomer = (int)cmd4.LastInsertedId;
                    }
                }

                var supplierInn = ((string)c.SelectToken("placer.mainInfo.inn") ?? "").Trim();
                if (!string.IsNullOrEmpty(supplierInn))
                {
                    var kppSupplier = ((string)c.SelectToken("placer.mainInfo.kpp") ?? "").Trim();
                    var selectSupplier =
                        $"SELECT id FROM {Program.Prefix}od_supplier WHERE inn = @supplier_inn AND kpp = @kpp_supplier";
                    var cmd5 = new MySqlCommand(selectSupplier, connect);
                    cmd5.Prepare();
                    cmd5.Parameters.AddWithValue("@supplier_inn", supplierInn);
                    cmd5.Parameters.AddWithValue("@kpp_supplier", kppSupplier);
                    var reader34 = cmd5.ExecuteReader();
                    var resRead = reader34.HasRows;
                    if (resRead)
                    {
                        reader34.Read();
                        idSupplier = reader34.GetInt32("id");
                        reader34.Close();
                    }
                    else
                    {
                        reader34.Close();
                        var contactphoneSupplier = "";

                        var contactemailSupplier = ((string)c.SelectToken("placer.mainInfo.email") ?? "").Trim();

                        var organizationnameSupplier =
                            ((string)c.SelectToken("placer.mainInfo.fullName") ?? "").Trim();

                        var contractsCountSupplier = 1;
                        var contractsSumSupplier = "";
                        var contracts223CountSupplier = 0;
                        var contracts223SumSupplier = 0.0m;
                        var ogrnSupplier = ((string)c.SelectToken("placer.mainInfo.ogrn") ?? "").Trim();
                        var regionCodeSupplier = "";
                        var postalAddressSupplier =
                            ((string)c.SelectToken("placer.mainInfo.postalAddress") ?? "").Trim();
                        var contactfaxSupplier = "";
                        var contactNameSupplier = "";
                        var addSupplier =
                            $"INSERT INTO {Program.Prefix}od_supplier SET inn = @supplier_inn, kpp = @kpp_supplier, " +
                            "contracts_count = @contracts_count, " +
                            "contracts223_count = @contracts223_count, contracts_sum = @contracts_sum, " +
                            "contracts223_sum = @contracts223_sum, ogrn = @ogrn,region_code = @region_code, " +
                            "organizationName = @organizationName,postal_address = @postal_address, " +
                            "contactPhone = @contactPhone,contactFax = @contactFax, " +
                            "contactEMail = @contactEMail,contact_name = @contact_name";
                        var cmd6 = new MySqlCommand(addSupplier, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@supplier_inn", supplierInn);
                        cmd6.Parameters.AddWithValue("@kpp_supplier", kppSupplier);
                        cmd6.Parameters.AddWithValue("@contracts_count", contractsCountSupplier);
                        cmd6.Parameters.AddWithValue("@contracts223_count", contracts223CountSupplier);
                        cmd6.Parameters.AddWithValue("@contracts_sum", contractsSumSupplier);
                        cmd6.Parameters.AddWithValue("@contracts223_sum", contracts223SumSupplier);
                        cmd6.Parameters.AddWithValue("@ogrn", ogrnSupplier);
                        cmd6.Parameters.AddWithValue("@region_code", regionCodeSupplier);
                        cmd6.Parameters.AddWithValue("@organizationName", organizationnameSupplier);
                        cmd6.Parameters.AddWithValue("@postal_address", postalAddressSupplier);
                        cmd6.Parameters.AddWithValue("@contactPhone", contactphoneSupplier);
                        cmd6.Parameters.AddWithValue("@contactFax", contactfaxSupplier);
                        cmd6.Parameters.AddWithValue("@contactEMail", contactemailSupplier);
                        cmd6.Parameters.AddWithValue("@contact_name", contactNameSupplier);
                        var addS = cmd6.ExecuteNonQuery();
                        idSupplier = (int)cmd6.LastInsertedId;
                    }
                }

                var regnum = ((string)c.SelectToken("registrationNumber") ?? "").Trim();
                var status = ((string)c.SelectToken("status") ?? "").Trim();
                var url = ((string)c.SelectToken("urlEIS") ?? "").Trim();
                var contrCreateDate = (DateTime?)c.SelectToken("createDateTime") ??
                                      DateTime.MinValue;
                var purchaseNoticeNumber =
                    ((string)c.SelectToken("purchaseNoticeInfo.purchaseNoticeNumber") ?? "").Trim();
                var price = (decimal?)c.SelectToken("price") ?? 0.0m;
                var currency = ((string)c.SelectToken("currency.code") ?? "").Trim();
                var startExecutionDate = (DateTime?)c.SelectToken("startExecutionDate") ??
                                         DateTime.MinValue;
                var endExecutionDate = (DateTime?)c.SelectToken("endExecutionDate") ??
                                       DateTime.MinValue;
                var typeEis = "contract";
                var d1 = c.SelectToken("purchaseTypeInfo")?.ToString() ?? "";
                var d2 = ((string)c.SelectToken("subjectContract") ?? "").Trim();
                var d3 = ((string)c.SelectToken("name") ?? "").Trim();
                var d4 = ((string)c.SelectToken("modificationDescription") ?? "").Trim();
                var dopInfo = c.ToString();
                var indexOfSubstring = dopInfo.IndexOf("placer");
                dopInfo = dopInfo.Substring(0, indexOfSubstring) + c.SelectToken("contractConfirmingDocs");
                var insertContract =
                    "insert into purchase_contracts223 set  guid = @guid, regnum = @regnum, current_contract_stage = @current_contract_stage, region_code = @region_code, url = @url, contr_create_date = @contr_create_date, create_date = @create_date,notification_number = @notification_number, contract_price = @contract_price, currency = @currency, version_number = @version_number, fulfillment_date = @fulfillment_date, id_customer = @id_customer, id_supplier = @id_supplier, cancel = @cancel, xml = @xml, address = @address, dop_info = @dop_info,startExecutionDate = @startExecutionDate, endExecutionDate = @endExecutionDate, type_eis = @type_eis";
                var cmd77 = new MySqlCommand(insertContract, connect);
                cmd77.Prepare();
                cmd77.Parameters.AddWithValue("@guid", guid);
                cmd77.Parameters.AddWithValue("@regnum", regnum);
                cmd77.Parameters.AddWithValue("@current_contract_stage", status);
                cmd77.Parameters.AddWithValue("@region_code", "");
                cmd77.Parameters.AddWithValue("@url", url);
                cmd77.Parameters.AddWithValue("@contr_create_date", contrCreateDate);
                cmd77.Parameters.AddWithValue("@create_date", contrCreateDate);
                cmd77.Parameters.AddWithValue("@notification_number", purchaseNoticeNumber);
                cmd77.Parameters.AddWithValue("@contract_price", price);
                cmd77.Parameters.AddWithValue("@currency", currency);
                cmd77.Parameters.AddWithValue("@version_number", version);
                cmd77.Parameters.AddWithValue("@fulfillment_date", "");
                cmd77.Parameters.AddWithValue("@id_customer", idCustomer);
                cmd77.Parameters.AddWithValue("@id_supplier", idSupplier);
                cmd77.Parameters.AddWithValue("@cancel", cancel);
                cmd77.Parameters.AddWithValue("@xml", xml);
                cmd77.Parameters.AddWithValue("@address", postalAddressCustomer);
                cmd77.Parameters.AddWithValue("@dop_info", dopInfo);
                cmd77.Parameters.AddWithValue("@startExecutionDate", startExecutionDate);
                cmd77.Parameters.AddWithValue("@endExecutionDate", endExecutionDate);
                cmd77.Parameters.AddWithValue("@type_eis", typeEis);
                var resCont = cmd77.ExecuteNonQuery();
                var idContr = (int)cmd77.LastInsertedId;
                AddTenderSign223?.Invoke(resCont);
                var products = GetElements(c, "contractPositions.contractPosition");
                foreach (var p in products)
                {
                    var guidP = ((string)p.SelectToken("guid") ?? "").Trim();
                    var name = ((string)p.SelectToken("name") ?? "").Trim();
                    var okpCode = ((string)p.SelectToken("okpd2.code") ?? "").Trim();
                    var okpName = ((string)p.SelectToken("okpd2.name") ?? "").Trim();
                    var quant = (decimal?)p.SelectToken("qty") ?? 0.0m;
                    var okei = ((string)p.SelectToken("okei.name") ?? "").Trim();
                    var dopInfoP = p.SelectToken("countryManufacturer")?.ToString() ?? "";
                    var countryOfOriginName = ((string)p.SelectToken("countriesOfOrigin.country.name") ?? "").Trim();
                    var countryOfOriginCode =
                        ((string)p.SelectToken("countriesOfOrigin.country.digitalCode") ?? "").Trim();
                    var unitPrice = (decimal?)p.SelectToken("unitPrice") ?? 0.0m;
                    var curName = ((string)p.SelectToken("currency.name") ?? "").Trim();
                    var curCode = ((string)p.SelectToken("currency.digitalCode") ?? "").Trim();
                    var insertP =
                        "insert into purchase_products223 set  id_purchase_contract = @id_purchase_contract, name = @name, okpd_code = @okpd_code, okpd_name = @okpd_name, okved_code = @okved_code, okved_name = @okved_name, quantity = @quantity, okei = @okei, dop_info = @dop_info, countryOfOrigin_name = @countryOfOrigin_name, countryOfOrigin_code = @countryOfOrigin_code, unit_Price = @unit_Price, currency_name = @currency_name, currency_code = @currency_code, guid = @guid";
                    var cmd88 = new MySqlCommand(insertP, connect);
                    cmd88.Prepare();
                    cmd88.Parameters.AddWithValue("@guid", guidP);
                    cmd88.Parameters.AddWithValue("@id_purchase_contract", idContr);
                    cmd88.Parameters.AddWithValue("@name", name);
                    cmd88.Parameters.AddWithValue("@okpd_code", okpCode);
                    cmd88.Parameters.AddWithValue("@okpd_name", okpName);
                    cmd88.Parameters.AddWithValue("@okved_code", "");
                    cmd88.Parameters.AddWithValue("@okved_name", "");
                    cmd88.Parameters.AddWithValue("@quantity", quant);
                    cmd88.Parameters.AddWithValue("@okei", okei);
                    cmd88.Parameters.AddWithValue("@dop_info", dopInfoP);
                    cmd88.Parameters.AddWithValue("@countryOfOrigin_name", countryOfOriginName);
                    cmd88.Parameters.AddWithValue("@countryOfOrigin_code", countryOfOriginCode);
                    cmd88.Parameters.AddWithValue("@unit_Price", unitPrice);
                    cmd88.Parameters.AddWithValue("@currency_name", curName);
                    cmd88.Parameters.AddWithValue("@currency_code", curCode);
                    var resCP = cmd88.ExecuteNonQuery();
                    var attachments = GetElements(c, "attachments.document");
                    foreach (var att in attachments)
                    {
                        var attachName = ((string)att.SelectToken("fileName") ?? "").Trim();
                        var attachDescription = ((string)att.SelectToken("description") ?? "").Trim();
                        var attachUrl = ((string)att.SelectToken("url") ?? "").Trim();
                        var insertAttach =
                            $"INSERT INTO {Program.Prefix}purchase_contracts223_attach SET id_purchase_contracts223 = @id_purchase_contracts223, file_name = @file_name, url = @url, description = @description";
                        var cmd9 = new MySqlCommand(insertAttach, connect);
                        cmd9.Prepare();
                        cmd9.Parameters.AddWithValue("@id_purchase_contracts223", idContr);
                        cmd9.Parameters.AddWithValue("@file_name", attachName);
                        cmd9.Parameters.AddWithValue("@url", attachUrl);
                        cmd9.Parameters.AddWithValue("@description", attachDescription);
                        cmd9.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
#region

using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace ParserTenders.TenderDir
{
    public class TenderSignProj44 : Tender
    {
        public TenderSignProj44(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTenderSignProj44 += delegate(int d)
            {
                if (d > 0)
                {
                    Program.AddTenderSignProj44++;
                }
                else
                {
                    Log.Logger("Не удалось добавить TenderSignProj44", FilePath);
                }
            };
        }

        public event Action<int> AddTenderSignProj44;

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var root = (JObject)T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.StartsWith("cpContractSign"));
            if (firstOrDefault is null)
            {
                Log.Logger("Не могу найти тег TenderSignProj44", FilePath);
                return;
            }

            var tender = firstOrDefault.Value;
            var purchaseNumber = ((string)tender.SelectToken("foundationInfo.purchaseNumber") ?? "").Trim();
            if (string.IsNullOrEmpty(purchaseNumber))
            {
                Log.Logger("Не могу найти purchaseNumber у sign", FilePath);
                //return;
            }

            using (var connect = ConnectToDb.GetDbConnection())
            {
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

                var idSign = ((string)tender.SelectToken("id") ?? "").Trim();
                var docPublishDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("commonInfo.publishDTInEIS") ?? "") ??
                     "").Trim('"');
                var selectSign =
                    $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign_project WHERE id_tender = @id_tender AND id_sign = @id_sign AND pub_date = @pub_date";
                var cmd1 = new MySqlCommand(selectSign, connect);
                cmd1.Prepare();
                cmd1.Parameters.AddWithValue("@id_tender", idTender);
                cmd1.Parameters.AddWithValue("@id_sign", idSign);
                cmd1.Parameters.AddWithValue("@pub_date", docPublishDate);
                var reader1 = cmd1.ExecuteReader();
                if (reader1.HasRows)
                {
                    reader1.Close();
                    return;
                }

                reader1.Close();
                var signNumber =
                    ((string)tender.SelectToken("commonInfo.number") ?? "").Trim();
                var protocolNumber =
                    ((string)tender.SelectToken("foundationInfo.protocolInfo.number") ?? "").Trim();
                var signDate = ((string)tender.SelectToken("commonInfo.signDate") ?? "").Trim();
                var contractSignPrice = ((string)tender.SelectToken("contractInfo.price") ?? "").Trim();
                contractSignPrice = contractSignPrice.Replace(",", ".");
                var signCurrency = ((string)tender.SelectToken("contractInfo.currency.name") ?? "").Trim();
                var customerRegNum = ((string)tender.SelectToken("customerInfo.regNum") ?? "").Trim();
                var protocoleDate =
                    (JsonConvert.SerializeObject(tender.SelectToken("foundationInfo.protocolInfo.publishDTInEIS") ??
                                                 "") ??
                     "").Trim('"');
                var typeSign = "ContractSign";
                var purchaseCode = ((string)tender.SelectToken("foundationInfo.purchaseCode") ?? "").Trim();
                var printForm = ((string)tender.SelectToken("printFormInfo.url") ?? "").Trim();
                if (!string.IsNullOrEmpty(printForm) && printForm.IndexOf("CDATA", StringComparison.Ordinal) != -1)
                {
                    printForm = printForm.Substring(9, printForm.Length - 12);
                }

                var (supplierContact, supplierInn,
                    supplierKpp, participantType, organizationName, countryFullName, factualAddress,
                    postAddress) = ("", "", "", "", "", "", "", "");
                var supplier = tender.SelectToken("participantInfo.legalEntityRFInfo") ??
                               tender.SelectToken("participantInfo.legalEntityForeignStateInfo") ??
                               tender.SelectToken("participantInfo.individualPersonRFInfo") ??
                               tender.SelectToken("participantInfo.individualPersonForeignStateInfo");
                if (supplier != null)
                {
                    supplierInn = ((string)supplier.SelectToken("INN") ?? "").Trim();
                    if (supplierInn == "")
                    {
                        supplierInn = ((string)supplier.SelectToken("taxPayerCode") ?? "").Trim();
                    }

                    supplierKpp = ((string)supplier.SelectToken("KPP") ?? "").Trim();
                    organizationName = ((string)supplier.SelectToken("fullName") ?? "").Trim();

                    if (organizationName == "")
                    {
                        var supplierLastName = ((string)supplier.SelectToken("nameInfo.lastName") ?? "")
                            .Trim();
                        var supplierFirstName = ((string)supplier.SelectToken("nameInfo.firstName") ?? "")
                            .Trim();
                        var supplierMiddleName = ((string)supplier.SelectToken("nameInfo.middleName") ?? "")
                            .Trim();
                        supplierContact = $"{supplierLastName} {supplierFirstName} {supplierMiddleName}".Trim();
                        organizationName = supplierContact;
                    }

                    countryFullName = ((string)supplier.SelectToken("country.countryFullName") ?? "").Trim();
                }

                var idCustomer = 0;
                if (!string.IsNullOrEmpty(customerRegNum))
                {
                    var selectCustomer =
                        $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                    var cmd2 = new MySqlCommand(selectCustomer, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@reg_num", customerRegNum);
                    var reader2 = cmd2.ExecuteReader();
                    if (reader2.HasRows)
                    {
                        reader2.Read();
                        idCustomer = reader2.GetInt32("id_customer");
                        reader2.Close();
                    }
                    else
                    {
                        reader2.Close();
                        var customerFullName = ((string)tender.SelectToken("customerInfo.fullName") ?? "").Trim();
                        var customerInn = ((string)tender.SelectToken("customerInfo.INN") ?? "").Trim();
                        var insertCustomer =
                            $"INSERT INTO {Program.Prefix}customer SET reg_num = @reg_num, full_name = @full_name, inn = @inn";
                        var cmd14 = new MySqlCommand(insertCustomer, connect);
                        cmd14.Prepare();
                        cmd14.Parameters.AddWithValue("@reg_num", customerRegNum);
                        cmd14.Parameters.AddWithValue("@full_name", customerFullName);
                        cmd14.Parameters.AddWithValue("@inn", customerInn);
                        cmd14.ExecuteNonQuery();
                        idCustomer = (int)cmd14.LastInsertedId;
                    }
                }
                else
                {
                    Log.Logger("У TenderSign нет customer_reg_num", FilePath);
                }

                var idSupplier = 0;
                if (!string.IsNullOrEmpty(supplierInn) || !string.IsNullOrEmpty(organizationName))
                {
                    if (!string.IsNullOrEmpty(supplierInn))
                    {
                        var selectSupplier =
                            $"SELECT id_supplier FROM {Program.Prefix}supplier WHERE inn_supplier = @inn_supplier AND kpp_supplier = @kpp_supplier";
                        var cmd3 = new MySqlCommand(selectSupplier, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@inn_supplier", supplierInn);
                        cmd3.Parameters.AddWithValue("@kpp_supplier", supplierKpp);
                        var reader3 = cmd3.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idSupplier = reader3.GetInt32("id_supplier");
                        }

                        reader3.Close();
                    }

                    if (!string.IsNullOrEmpty(organizationName) && idSupplier == 0)
                    {
                        var selectSupplier =
                            $"SELECT id_supplier FROM {Program.Prefix}supplier WHERE organization_name = @organization_name";
                        var cmd3 = new MySqlCommand(selectSupplier, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@organization_name", organizationName);
                        var reader3 = cmd3.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idSupplier = reader3.GetInt32("id_supplier");
                        }

                        reader3.Close();
                    }

                    if (idSupplier == 0)
                    {
                        var insertSupplier =
                            $"INSERT INTO {Program.Prefix}supplier SET participant_type = @participant_type, inn_supplier = @inn_supplier, kpp_supplier = @kpp_supplier, organization_name = @organization_name, country_full_name = @country_full_name, factual_address = @factual_address, post_address = @post_address, contact = @contact";
                        var cmd4 = new MySqlCommand(insertSupplier, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@inn_supplier", supplierInn);
                        cmd4.Parameters.AddWithValue("@kpp_supplier", supplierKpp);
                        cmd4.Parameters.AddWithValue("@participant_type", participantType);
                        cmd4.Parameters.AddWithValue("@organization_name", organizationName);
                        cmd4.Parameters.AddWithValue("@country_full_name", countryFullName);
                        cmd4.Parameters.AddWithValue("@factual_address", factualAddress);
                        cmd4.Parameters.AddWithValue("@post_address", postAddress);
                        cmd4.Parameters.AddWithValue("@contact", supplierContact);
                        cmd4.ExecuteNonQuery();
                        idSupplier = (int)cmd4.LastInsertedId;
                    }
                }

                var insertContract =
                    $"INSERT INTO {Program.Prefix}contract_sign_project SET id_tender = @id_tender, id_sign = @id_sign, purchase_number = @purchase_number, sign_number = @sign_number, sign_date = @sign_date, id_customer = @id_customer, customer_reg_num = @customer_reg_num, id_supplier = @id_supplier, contract_sign_price = @contract_sign_price, sign_currency = @sign_currency, protocole_date = @protocole_date, supplier_contact = @supplier_contact, xml = @xml, type_sign = @type_sign, print_form = @print_form, purchase_code = @purchase_code, pub_date = @pub_date, protocol_number = @protocol_number";
                var cmd5 = new MySqlCommand(insertContract, connect);
                cmd5.Prepare();
                cmd5.Parameters.AddWithValue("@id_tender", idTender);
                cmd5.Parameters.AddWithValue("@id_sign", idSign);
                cmd5.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                cmd5.Parameters.AddWithValue("@sign_number", signNumber);
                cmd5.Parameters.AddWithValue("@sign_date", signDate);
                cmd5.Parameters.AddWithValue("@id_customer", idCustomer);
                cmd5.Parameters.AddWithValue("@customer_reg_num", customerRegNum);
                cmd5.Parameters.AddWithValue("@id_supplier", idSupplier);
                cmd5.Parameters.AddWithValue("@contract_sign_price", contractSignPrice);
                cmd5.Parameters.AddWithValue("@sign_currency", signCurrency);
                cmd5.Parameters.AddWithValue("@protocole_date", protocoleDate);
                cmd5.Parameters.AddWithValue("@supplier_contact", supplierContact);
                cmd5.Parameters.AddWithValue("@xml", xml);
                cmd5.Parameters.AddWithValue("@type_sign", typeSign);
                cmd5.Parameters.AddWithValue("@print_form", printForm);
                cmd5.Parameters.AddWithValue("@purchase_code", purchaseCode);
                cmd5.Parameters.AddWithValue("@pub_date", docPublishDate);
                cmd5.Parameters.AddWithValue("@protocol_number", protocolNumber);
                var resCont = cmd5.ExecuteNonQuery();
                var idSign44 = (int)cmd5.LastInsertedId;
                AddTenderSignProj44?.Invoke(resCont);
                var attachments = GetElements(tender, "contractProjectFilesInfo.contractProjectFileInfo");
                foreach (var att in attachments)
                {
                    var attachName = ((string)att.SelectToken("fileName") ?? "").Trim();
                    var attachDescription = ((string)att.SelectToken("docDescription") ?? "").Trim();
                    var attachUrl = ((string)att.SelectToken("url") ?? "").Trim();
                    if (!string.IsNullOrEmpty(attachName))
                    {
                        var insertAttach =
                            $"INSERT INTO {Program.Prefix}contract_sign_project_attach SET id_contract_sign_project = @id_contract_sign_project, file_name = @file_name, url = @url, description = @description";
                        var cmd11 = new MySqlCommand(insertAttach, connect);
                        cmd11.Prepare();
                        cmd11.Parameters.AddWithValue("@id_contract_sign_project", idSign44);
                        cmd11.Parameters.AddWithValue("@file_name", attachName);
                        cmd11.Parameters.AddWithValue("@url", attachUrl);
                        cmd11.Parameters.AddWithValue("@description", attachDescription);
                        cmd11.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
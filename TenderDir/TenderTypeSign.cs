using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
{
    public class TenderTypeSign : Tender
    {
        public event Action<int> AddTenderSign;

        public TenderTypeSign(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddTenderSign += delegate(int d)
            {
                if (d > 0)
                    Program.AddTenderSign++;
                else
                    Log.Logger("Не удалось добавить TenderSign", FilePath);
            };
        }

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            var root = (JObject) T.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                var tender = firstOrDefault.Value;
                var purchaseNumber = ((string) tender.SelectToken("foundation.order.purchaseNumber") ?? "").Trim();
                if (string.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у sign", FilePath);
                    //return;
                }
                else
                {
                    if (purchaseNumber.StartsWith("9", StringComparison.Ordinal))
                    {
                        /*Log.Logger("Тестовый тендер sign", purchaseNumber, file_path);*/
                        return;
                    }
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

                    var idSign = ((string) tender.SelectToken("id") ?? "").Trim();
                    var selectSign =
                        $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign WHERE id_tender = @id_tender AND id_sign = @id_sign";
                    var cmd1 = new MySqlCommand(selectSign, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_tender", idTender);
                    cmd1.Parameters.AddWithValue("@id_sign", idSign);
                    var reader1 = cmd1.ExecuteReader();
                    if (reader1.HasRows)
                    {
                        reader1.Close();
                        return;
                    }

                    reader1.Close();
                    var signNumber =
                        ((string) tender.SelectToken("foundation.order.foundationProtocolNumber") ?? "").Trim();
                    var signDate = ((string) tender.SelectToken("signDate") ?? "").Trim();
                    var customerRegNum = ((string) tender.SelectToken("customer.regNum") ?? "").Trim();
                    var contractSignPrice = ((string) tender.SelectToken("price") ?? "").Trim();
                    contractSignPrice = contractSignPrice.Replace(",", ".");
                    var signCurrency = ((string) tender.SelectToken("currency.name") ?? "").Trim();
                    var concludeContractRight = ((string) tender.SelectToken("concludeContractRight") ?? "")
                        .Trim();
                    var protocoleDate = ((string) tender.SelectToken("protocolDate") ?? "").Trim();
                    var (supplierContact, supplierEmail, supplierContactPhone, supplierContactFax, supplierInn,
                        supplierKpp, participantType, organizationName, countryFullName, factualAddress,
                        postAddress) = ("", "", "", "", "", "", "", "", "", "", "");
                    var suppliers = GetElements(tender, "suppliers.supplier");
                    if (suppliers.Count != 0)
                    {
                        var supplierLastName = ((string) suppliers[0].SelectToken("contactInfo.lastName") ?? "")
                            .Trim();
                        var supplierFirstName = ((string) suppliers[0].SelectToken("contactInfo.firstName") ?? "")
                            .Trim();
                        var supplierMiddleName = ((string) suppliers[0].SelectToken("contactInfo.middleName") ?? "")
                            .Trim();
                        supplierContact = $"{supplierLastName} {supplierFirstName} {supplierMiddleName}".Trim();
                        supplierEmail = ((string) suppliers[0].SelectToken("contactEMail") ?? "").Trim();
                        supplierContactPhone = ((string) suppliers[0].SelectToken("contactPhone") ?? "").Trim();
                        supplierContactFax = ((string) suppliers[0].SelectToken("contactFax") ?? "").Trim();
                        supplierInn = ((string) suppliers[0].SelectToken("inn") ?? "").Trim();
                        supplierKpp = ((string) suppliers[0].SelectToken("kpp") ?? "").Trim();
                        participantType = ((string) suppliers[0].SelectToken("participantType") ?? "").Trim();
                        organizationName = ((string) suppliers[0].SelectToken("organizationName") ?? "").Trim();
                        countryFullName = ((string) suppliers[0].SelectToken("country.countryFullName") ?? "").Trim();
                        factualAddress = ((string) suppliers[0].SelectToken("factualAddress") ?? "").Trim();
                        postAddress = ((string) suppliers[0].SelectToken("postAddress") ?? "").Trim();
                    }
                    else
                    {
                        Log.Logger("У TenderSign нет supplier", FilePath);
                    }
                    if (suppliers.Count > 1)
                    {
                        Log.Logger("У TenderSign несколько supplier", FilePath);
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
                            Log.Logger("У TenderSign нет id_customer", FilePath);
                        }
                    }
                    else
                    {
                        Log.Logger("У TenderSign нет customer_reg_num", FilePath);
                    }

                    var idSupplier = 0;
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
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            var insertSupplier =
                                $"INSERT INTO {Program.Prefix}supplier SET participant_type = @participant_type, inn_supplier = @inn_supplier, kpp_supplier = @kpp_supplier, organization_name = @organization_name, country_full_name = @country_full_name, factual_address = @factual_address, post_address = @post_address, contact = @contact, email = @email, phone = @phone, fax = @fax";
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
                            cmd4.Parameters.AddWithValue("@email", supplierEmail);
                            cmd4.Parameters.AddWithValue("@phone", supplierContactPhone);
                            cmd4.Parameters.AddWithValue("@fax", supplierContactFax);
                            cmd4.ExecuteNonQuery();
                            idSupplier = (int) cmd4.LastInsertedId;
                        }
                    }
                    else
                    {
                        //Log.Logger("Нет supplier_inn в TenderSign", FilePath);
                    }

                    var insertContract =
                        $"INSERT INTO {Program.Prefix}contract_sign SET id_tender = @id_tender, id_sign = @id_sign, purchase_number = @purchase_number, sign_number = @sign_number, sign_date = @sign_date, id_customer = @id_customer, customer_reg_num = @customer_reg_num, id_supplier = @id_supplier, contract_sign_price = @contract_sign_price, sign_currency = @sign_currency, conclude_contract_right = @conclude_contract_right, protocole_date = @protocole_date, supplier_contact = @supplier_contact, supplier_email = @supplier_email, supplier_contact_phone = @supplier_contact_phone, supplier_contact_fax = @supplier_contact_fax, xml = @xml";
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
                    cmd5.Parameters.AddWithValue("@conclude_contract_right", concludeContractRight);
                    cmd5.Parameters.AddWithValue("@protocole_date", protocoleDate);
                    cmd5.Parameters.AddWithValue("@supplier_contact", supplierContact);
                    cmd5.Parameters.AddWithValue("@supplier_email", supplierEmail);
                    cmd5.Parameters.AddWithValue("@supplier_contact_phone", supplierContactPhone);
                    cmd5.Parameters.AddWithValue("@supplier_contact_fax", supplierContactFax);
                    cmd5.Parameters.AddWithValue("@xml", xml);
                    var resCont = cmd5.ExecuteNonQuery();
                    AddTenderSign?.Invoke(resCont);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderSign", FilePath);
            }
        }
    }
}
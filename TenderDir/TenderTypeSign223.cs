using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders.TenderDir
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
            int upd = 0;
            JObject c = (JObject) T.SelectToken("contract.body.item.contractData");
            if (!c.IsNullOrEmpty())
            {
                string purchaseNumber =
                    ((string) c.SelectToken("purchaseNoticeInfo.purchaseNoticeNumber") ?? "").Trim();
                //Console.WriteLine(purchaseNumber);
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    //Log.Logger("Не могу найти purchaseNumber у sign223", FilePath);
                    //return;
                }

                using (MySqlConnection connect = ConnectToDb.GetDbConnection())
                {
                    int idTender = 0;
                    connect.Open();
                    string selectTender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    MySqlCommand cmd = new MySqlCommand(selectTender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
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

                    string idSign = ((string) c.SelectToken("guid") ?? "").Trim();
                    string selectSign =
                        $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign WHERE id_tender = @id_tender AND id_sign = @id_sign";
                    MySqlCommand cmd1 = new MySqlCommand(selectSign, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_tender", idTender);
                    cmd1.Parameters.AddWithValue("@id_sign", idSign);
                    MySqlDataReader reader1 = cmd1.ExecuteReader();
                    if (reader1.HasRows)
                    {
                        reader1.Close();
                        return;
                    }

                    reader1.Close();

                    //Console.WriteLine(idcSign);
                    string signNumber = ((string) c.SelectToken("contractRegNumber") ?? "").Trim();
                    int idcSignNumber = 0;
                    string selectSignNum =
                        $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign WHERE purchase_number = @purchase_number AND sign_number = @sign_number";
                    MySqlCommand cmd22 = new MySqlCommand(selectSignNum, connect);
                    cmd22.Prepare();
                    cmd22.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd22.Parameters.AddWithValue("@sign_number", signNumber);
                    MySqlDataReader reader22 = cmd22.ExecuteReader();
                    if (reader22.HasRows)
                    {
                        reader22.Read();
                        idcSignNumber = reader22.GetInt32("id_contract_sign");
                        reader22.Close();
                    }

                    reader22.Close();
                    if (idcSignNumber != 0)
                        upd = 1;
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
                            postAddress, regSup, citySup, streetSup) =
                        ("", "", "", "", "", "", "", "", "", "", "", "", "", "");
                    var suppl = GetElements(c, "supplierInfo");
                    if (suppl.Count > 0)
                    {
                        var sp = suppl[0];
                        supplierEmail = ((string) sp.SelectToken("address.email") ?? "").Trim();
                        supplierContactPhone = ((string) sp.SelectToken("address.phone") ?? "").Trim();
                        supplierContactFax = ((string) sp.SelectToken("address.fax") ?? "").Trim();
                        supplierInn = ((string) sp.SelectToken("inn") ?? "").Trim();
                        if (String.IsNullOrEmpty(supplierInn))
                        {
                            supplierInn = ((string) sp.SelectToken("code") ?? "").Trim();
                        }

                        supplierKpp = ((string) sp.SelectToken("kpp") ?? "").Trim();
                        participantType = ((string) sp.SelectToken("type") ?? "").Trim();
                        organizationName = ((string) sp.SelectToken("name") ?? "").Trim();
                        countryFullName = ((string) sp.SelectToken("address.country.name") ?? "").Trim();
                        regSup = ((string) sp.SelectToken("address.region.name") ?? "").Trim();
                        if (String.IsNullOrEmpty(regSup))
                        {
                            regSup = ((string) sp.SelectToken("address.region.fullName") ?? "").Trim();
                        }

                        citySup = ((string) sp.SelectToken("address.city").CheckIsObjOrString() ?? "").Trim();
                        if (String.IsNullOrEmpty(citySup))
                        {
                            citySup = ((string) sp.SelectToken("address.city.fullName") ?? "").Trim();
                        }

                        streetSup = ((string) sp.SelectToken("address.street").CheckIsObjOrString() ?? "").Trim();
                        if (String.IsNullOrEmpty(streetSup))
                        {
                            streetSup = ((string) sp.SelectToken("address.street.fullName") ?? "").Trim();
                        }

                        factualAddress = $"{regSup} {citySup} {streetSup}".Trim();
                    }

                    int idCustomer = 0;
                    if (!String.IsNullOrEmpty(customerInn))
                    {
                        string selectCustomer =
                            $"SELECT id_customer FROM {Program.Prefix}customer WHERE inn = @inn";
                        MySqlCommand cmd2 = new MySqlCommand(selectCustomer, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@inn", customerInn);
                        MySqlDataReader reader2 = cmd2.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            idCustomer = reader2.GetInt32("id_customer");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            //Log.Logger("У TenderSign нет id_customer", FilePath);
                        }
                    }
                    else
                    {
                        Log.Logger("У TenderSign223 нет customer_inn", FilePath);
                    }

                    int idSupplier = 0;
                    if (!String.IsNullOrEmpty(supplierInn))
                    {
                        string selectSupplier =
                            $"SELECT id_supplier FROM {Program.Prefix}supplier WHERE inn_supplier = @inn_supplier AND kpp_supplier = @kpp_supplier";
                        MySqlCommand cmd3 = new MySqlCommand(selectSupplier, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@inn_supplier", supplierInn);
                        cmd3.Parameters.AddWithValue("@kpp_supplier", supplierKpp);
                        MySqlDataReader reader3 = cmd3.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idSupplier = reader3.GetInt32("id_supplier");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            string insertSupplier =
                                $"INSERT INTO {Program.Prefix}supplier SET participant_type = @participant_type, inn_supplier = @inn_supplier, kpp_supplier = @kpp_supplier, organization_name = @organization_name, country_full_name = @country_full_name, factual_address = @factual_address, post_address = @post_address, contact = @contact, email = @email, phone = @phone, fax = @fax";
                            MySqlCommand cmd4 = new MySqlCommand(insertSupplier, connect);
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
                        //Log.Logger("Нет supplier_inn в TenderSign223", FilePath);
                    }

                    if (upd == 0)
                    {
                        string insertContract =
                            $"INSERT INTO {Program.Prefix}contract_sign SET id_tender = @id_tender, id_sign = @id_sign, purchase_number = @purchase_number, sign_number = @sign_number, sign_date = @sign_date, id_customer = @id_customer, customer_reg_num = @customer_reg_num, id_supplier = @id_supplier, contract_sign_price = @contract_sign_price, sign_currency = @sign_currency, conclude_contract_right = @conclude_contract_right, protocole_date = @protocole_date, supplier_contact = @supplier_contact, supplier_email = @supplier_email, supplier_contact_phone = @supplier_contact_phone, supplier_contact_fax = @supplier_contact_fax, xml = @xml";
                        MySqlCommand cmd5 = new MySqlCommand(insertContract, connect);
                        cmd5.Prepare();
                        cmd5.Parameters.AddWithValue("@id_tender", idTender);
                        cmd5.Parameters.AddWithValue("@id_sign", idSign);
                        cmd5.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd5.Parameters.AddWithValue("@sign_number", signNumber);
                        cmd5.Parameters.AddWithValue("@sign_date", signDate);
                        cmd5.Parameters.AddWithValue("@id_customer", idCustomer);
                        cmd5.Parameters.AddWithValue("@customer_reg_num", "");
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
                        int resCont = cmd5.ExecuteNonQuery();
                        AddTenderSign223?.Invoke(resCont);
                    }
                    else
                    {
                        string insertContract =
                            $"UPDATE {Program.Prefix}contract_sign SET id_tender = @id_tender, id_sign = @id_sign, purchase_number = @purchase_number, sign_number = @sign_number, sign_date = @sign_date, id_customer = @id_customer, customer_reg_num = @customer_reg_num, id_supplier = @id_supplier, contract_sign_price = @contract_sign_price, sign_currency = @sign_currency, conclude_contract_right = @conclude_contract_right, protocole_date = @protocole_date, supplier_contact = @supplier_contact, supplier_email = @supplier_email, supplier_contact_phone = @supplier_contact_phone, supplier_contact_fax = @supplier_contact_fax, xml = @xml WHERE id_contract_sign = @id_contract_sign";
                        MySqlCommand cmd5 = new MySqlCommand(insertContract, connect);
                        cmd5.Prepare();
                        cmd5.Parameters.AddWithValue("@id_tender", idTender);
                        cmd5.Parameters.AddWithValue("@id_sign", idSign);
                        cmd5.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                        cmd5.Parameters.AddWithValue("@sign_number", signNumber);
                        cmd5.Parameters.AddWithValue("@sign_date", signDate);
                        cmd5.Parameters.AddWithValue("@id_customer", idCustomer);
                        cmd5.Parameters.AddWithValue("@customer_reg_num", "");
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
                        cmd5.Parameters.AddWithValue("@id_contract_sign", idcSignNumber);
                        int resCont = cmd5.ExecuteNonQuery();
                        UpdateTenderSign223?.Invoke(resCont);
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
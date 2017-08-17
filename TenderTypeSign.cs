using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class TenderTypeSign : Tender
    {
        public event Action<int> AddTenderSign;

        public TenderTypeSign(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddTenderSign += delegate(int d)
            {
                if (d > 0)
                    Program.AddTenderSign++;
                else
                    Log.Logger("Не удалось добавить TenderSign", file_path);
            };
        }

        public override void Parsing()
        {
            string xml = GetXml(file.ToString());
            JObject root = (JObject) t.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(p => p.Name.Contains("fcs"));
            if (firstOrDefault != null)
            {
                JToken tender = firstOrDefault.Value;
                string purchaseNumber = ((string) tender.SelectToken("foundation.order.purchaseNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(purchaseNumber))
                {
                    Log.Logger("Не могу найти purchaseNumber у sign", file_path);
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

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    int id_tender = 0;
                    connect.Open();
                    string select_tender =
                        $"SELECT id_tender FROM {Program.Prefix}tender WHERE id_region = @id_region AND purchase_number = @purchase_number AND cancel=0";
                    MySqlCommand cmd = new MySqlCommand(select_tender, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        id_tender = reader.GetInt32("id_tender");
                        reader.Close();
                    }
                    else
                    {
                        reader.Close();
                        //return;
                    }

                    string id_sign = ((string) tender.SelectToken("id") ?? "").Trim();
                    string select_sign =
                        $"SELECT id_contract_sign FROM {Program.Prefix}contract_sign WHERE id_tender = @id_tender AND id_sign = @id_sign";
                    MySqlCommand cmd1 = new MySqlCommand(select_sign, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@id_tender", id_tender);
                    cmd1.Parameters.AddWithValue("@id_sign", id_sign);
                    MySqlDataReader reader1 = cmd1.ExecuteReader();
                    if (reader1.HasRows)
                    {
                        reader1.Close();
                        return;
                    }

                    reader1.Close();
                    string sign_number =
                        ((string) tender.SelectToken("foundation.order.foundationProtocolNumber") ?? "").Trim();
                    string sign_date = ((string) tender.SelectToken("signDate") ?? "").Trim();
                    string customer_reg_num = ((string) tender.SelectToken("customer.regNum") ?? "").Trim();
                    string contract_sign_price = ((string) tender.SelectToken("price") ?? "").Trim();
                    contract_sign_price = contract_sign_price.Replace(",", ".");
                    string sign_currency = ((string) tender.SelectToken("currency.name") ?? "").Trim();
                    string conclude_contract_right = ((string) tender.SelectToken("concludeContractRight") ?? "")
                        .Trim();
                    string protocole_date = ((string) tender.SelectToken("protocolDate") ?? "").Trim();
                    var (supplier_contact, supplier_email, supplier_contact_phone, supplier_contact_fax, supplier_inn,
                        supplier_kpp, participant_type, organization_name, country_full_name, factual_address,
                        post_address) = ("", "", "", "", "", "", "", "", "", "", "");
                    List<JToken> suppliers = GetElements(tender, "suppliers.supplier");
                    if (suppliers.Count != 0)
                    {
                        string supplier_lastName = ((string) suppliers[0].SelectToken("contactInfo.lastName") ?? "")
                            .Trim();
                        string supplier_firstName = ((string) suppliers[0].SelectToken("contactInfo.firstName") ?? "")
                            .Trim();
                        string supplier_middleName = ((string) suppliers[0].SelectToken("contactInfo.middleName") ?? "")
                            .Trim();
                        supplier_contact = $"{supplier_lastName} {supplier_firstName} {supplier_middleName}".Trim();
                        supplier_email = ((string) suppliers[0].SelectToken("contactEMail") ?? "").Trim();
                        supplier_contact_phone = ((string) suppliers[0].SelectToken("contactPhone") ?? "").Trim();
                        supplier_contact_fax = ((string) suppliers[0].SelectToken("contactFax") ?? "").Trim();
                        supplier_inn = ((string) suppliers[0].SelectToken("inn") ?? "").Trim();
                        supplier_kpp = ((string) suppliers[0].SelectToken("kpp") ?? "").Trim();
                        participant_type = ((string) suppliers[0].SelectToken("participantType") ?? "").Trim();
                        organization_name = ((string) suppliers[0].SelectToken("organizationName") ?? "").Trim();
                        country_full_name = ((string) suppliers[0].SelectToken("country.countryFullName") ?? "").Trim();
                        factual_address = ((string) suppliers[0].SelectToken("factualAddress") ?? "").Trim();
                        post_address = ((string) suppliers[0].SelectToken("postAddress") ?? "").Trim();
                    }
                    else
                    {
                        Log.Logger("У TenderSign нет supplier", file_path);
                    }
                    if (suppliers.Count > 1)
                    {
                        Log.Logger("У TenderSign несколько supplier", file_path);
                    }
                    int id_customer = 0;
                    if (!String.IsNullOrEmpty(customer_reg_num))
                    {
                        string select_customer =
                            $"SELECT id_customer FROM {Program.Prefix}customer WHERE reg_num = @reg_num";
                        MySqlCommand cmd2 = new MySqlCommand(select_customer, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@reg_num", customer_reg_num);
                        MySqlDataReader reader2 = cmd2.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            id_customer = reader2.GetInt32("id_customer");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            Log.Logger("У TenderSign нет id_customer", file_path);
                        }
                    }
                    else
                    {
                        Log.Logger("У TenderSign нет customer_reg_num", file_path);
                    }

                    int id_supplier = 0;
                    if (!String.IsNullOrEmpty(supplier_inn))
                    {
                        string select_supplier =
                            $"SELECT id_supplier FROM {Program.Prefix}supplier WHERE inn_supplier = @inn_supplier AND kpp_supplier = @kpp_supplier";
                        MySqlCommand cmd3 = new MySqlCommand(select_supplier, connect);
                        cmd3.Prepare();
                        cmd3.Parameters.AddWithValue("@inn_supplier", supplier_inn);
                        cmd3.Parameters.AddWithValue("@kpp_supplier", supplier_kpp);
                        MySqlDataReader reader3 = cmd3.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            id_supplier = reader3.GetInt32("id_supplier");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            string insert_supplier =
                                $"INSERT INTO {Program.Prefix}supplier SET participant_type = @participant_type, inn_supplier = @inn_supplier, kpp_supplier = @kpp_supplier, organization_name = @organization_name, country_full_name = @country_full_name, factual_address = @factual_address, post_address = @post_address, contact = @contact, email = @email, phone = @phone, fax = @fax";
                            MySqlCommand cmd4 = new MySqlCommand(insert_supplier, connect);
                            cmd4.Prepare();
                            cmd4.Parameters.AddWithValue("@inn_supplier", supplier_inn);
                            cmd4.Parameters.AddWithValue("@kpp_supplier", supplier_kpp);
                            cmd4.Parameters.AddWithValue("@participant_type", participant_type);
                            cmd4.Parameters.AddWithValue("@organization_name", organization_name);
                            cmd4.Parameters.AddWithValue("@country_full_name", country_full_name);
                            cmd4.Parameters.AddWithValue("@factual_address", factual_address);
                            cmd4.Parameters.AddWithValue("@post_address", post_address);
                            cmd4.Parameters.AddWithValue("@contact", supplier_contact);
                            cmd4.Parameters.AddWithValue("@email", supplier_email);
                            cmd4.Parameters.AddWithValue("@phone", supplier_contact_phone);
                            cmd4.Parameters.AddWithValue("@fax", supplier_contact_fax);
                            cmd4.ExecuteNonQuery();
                            id_supplier = (int) cmd4.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет supplier_inn в TenderSign", file_path);
                    }

                    string insert_contract =
                        $"INSERT INTO {Program.Prefix}contract_sign SET id_tender = @id_tender, id_sign = @id_sign, purchase_number = @purchase_number, sign_number = @sign_number, sign_date = @sign_date, id_customer = @id_customer, customer_reg_num = @customer_reg_num, id_supplier = @id_supplier, contract_sign_price = @contract_sign_price, sign_currency = @sign_currency, conclude_contract_right = @conclude_contract_right, protocole_date = @protocole_date, supplier_contact = @supplier_contact, supplier_email = @supplier_email, supplier_contact_phone = @supplier_contact_phone, supplier_contact_fax = @supplier_contact_fax, xml = @xml";
                    MySqlCommand cmd5 = new MySqlCommand(insert_contract, connect);
                    cmd5.Prepare();
                    cmd5.Parameters.AddWithValue("@id_tender", id_tender);
                    cmd5.Parameters.AddWithValue("@id_sign", id_sign);
                    cmd5.Parameters.AddWithValue("@purchase_number", purchaseNumber);
                    cmd5.Parameters.AddWithValue("@sign_number", sign_number);
                    cmd5.Parameters.AddWithValue("@sign_date", sign_date);
                    cmd5.Parameters.AddWithValue("@id_customer", id_customer);
                    cmd5.Parameters.AddWithValue("@customer_reg_num", customer_reg_num);
                    cmd5.Parameters.AddWithValue("@id_supplier", id_supplier);
                    cmd5.Parameters.AddWithValue("@contract_sign_price", contract_sign_price);
                    cmd5.Parameters.AddWithValue("@sign_currency", sign_currency);
                    cmd5.Parameters.AddWithValue("@conclude_contract_right", conclude_contract_right);
                    cmd5.Parameters.AddWithValue("@protocole_date", protocole_date);
                    cmd5.Parameters.AddWithValue("@supplier_contact", supplier_contact);
                    cmd5.Parameters.AddWithValue("@supplier_email", supplier_email);
                    cmd5.Parameters.AddWithValue("@supplier_contact_phone", supplier_contact_phone);
                    cmd5.Parameters.AddWithValue("@supplier_contact_fax", supplier_contact_fax);
                    cmd5.Parameters.AddWithValue("@xml", xml);
                    int res_cont = cmd5.ExecuteNonQuery();
                    AddTenderSign?.Invoke(res_cont);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег TenderSign", file_path);
            }
        }
    }
}
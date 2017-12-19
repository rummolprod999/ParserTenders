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
                            $"SELECT id_contract_sign FROM {Program.TableContractsSign} WHERE id_tender = @id_tender AND id_sign = @id_sign",
                            paramidTender, paramGuid).FirstOrDefault();
                    if (idcSign != 0)
                    {
                        return;
                    }
                        
                    //Console.WriteLine(idcSign);
                    string signNumber = ((string) c.SelectToken("contractRegNumber") ?? "").Trim();
                    MySqlParameter paramSignNumber = new MySqlParameter("@sign_number", signNumber);
                    int idcSignNumber = db.Database
                        .SqlQuery<int>(
                            $"SELECT id_contract_sign FROM {Program.TableContractsSign} WHERE purchase_number = @purchase_number AND sign_number = @sign_number",
                            paramPurchaseNumber, paramSignNumber).FirstOrDefault();
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
                        postAddress, regSup, citySup, streetSup) = ("", "", "", "", "", "", "", "", "", "", "", "", "", "");
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
                        MySqlParameter paramcustomerInn = new MySqlParameter("@inn", customerInn);
                        idCustomer = db.Database
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
                            sup = new Supplier
                            {
                                ParticipiantType = participantType,
                                InnSupplier = supplierInn,
                                KppSupplier = supplierKpp,
                                OrganizationName = organizationName,
                                CountryFullName = countryFullName,
                                FactualAddress = factualAddress,
                                PostAddress = postAddress,
                                Contact = supplierContact,
                                Email = supplierEmail,
                                Phone = supplierContactPhone,
                                Fax = supplierContactFax
                            };
                            db.Suppliers.Add(sup);
                            db.SaveChanges();
                        }
                    }
                    else
                    {
                        Log.Logger("Нет supplier_inn в TenderSign223", FilePath);
                    }
                    ContractSign ts = null;
                    if (upd == 1)
                    {
                        ts = db.ContractsSign.FirstOrDefault(p => p.Id == idcSignNumber);
                        if (ts != null)
                        {
                            ts.IdTender = idTender;
                            ts.IdSign = idSign;
                            ts.PurchaseNumber = purchaseNumber;
                            ts.SignNumber = signNumber;
                            ts.SignDate = signDate;
                            ts.IdCustomer = idCustomer;
                            ts.CustomerRegNum = "";
                            if (sup == null)
                            {
                                ts.SupplierId = idSupplier;
                            }
                            else
                            {
                                ts.Supplier = sup;
                            }
                            ts.ContractSignPrice = contractSignPrice;
                            ts.SignCurrency = signCurrency;
                            ts.ConcludeContractRight = concludeContractRight;
                            ts.ProtocolDate = protocoleDate;
                            ts.SupplierContact = supplierContact;
                            ts.SupplierEmail = supplierEmail;
                            ts.SupplierContactPhone = supplierContactPhone;
                            ts.SupplierContactFax = supplierContactFax;
                            ts.Xml = xml;
                            db.Entry(ts).State = EntityState.Modified;
                            db.SaveChanges();
                            UpdateTenderSign223?.Invoke(1);
                        }
                    }
                    else
                    {
                        ts = new ContractSign
                        {
                            IdTender = idTender,
                            IdSign = idSign,
                            PurchaseNumber = purchaseNumber,
                            SignNumber = signNumber,
                            SignDate = signDate,
                            IdCustomer = idCustomer,
                            CustomerRegNum = "",
                            ContractSignPrice = contractSignPrice,
                            SignCurrency = signCurrency,
                            ConcludeContractRight = concludeContractRight,
                            ProtocolDate = protocoleDate,
                            SupplierContact = supplierContact,
                            SupplierEmail = supplierEmail,
                            SupplierContactPhone = supplierContactPhone,
                            SupplierContactFax = supplierContactFax,
                            Xml = xml,
                        };
                        if (sup == null)
                        {
                            ts.SupplierId = idSupplier;
                        }
                        else
                        {
                            ts.Supplier = sup;
                        }
                        db.ContractsSign.Add(ts);
                        db.SaveChanges();
                        AddTenderSign223?.Invoke(1);
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
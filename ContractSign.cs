using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParserTenders
{
    public class ContractSign
    {
        [Key, DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Column("id_contract_sign")]
        public int Id { get; set; }
        
        [Column("id_tender")]
        public int IdTender { get; set; }
        
        [Column("id_sign")]
        public string IdSign { get; set; }
        
        [Column("purchase_number")]
        public string PurchaseNumber { get; set; }
        
        [Column("sign_number")]
        public string SignNumber { get; set; }
        
        [Column("sign_date")]
        public DateTime SignDate { get; set; }
        
        [Column("id_customer")]
        public int IdCustomer { get; set; }
        
        [Column("customer_reg_num")]
        public string CustomerRegNum { get; set; }
        
        [Column("id_supplier")]
        public int? SupplierId { get; set; }
        
        public Supplier Supplier { get; set; }
        
        [Column("contract_sign_price")]
        public decimal ContractSignPrice { get; set; }
        
        [Column("sign_currency")]
        public string SignCurrency { get; set; }
        
        [Column("conclude_contract_right")]
        public int ConcludeContractRight { get; set; }
        
        [Column("protocole_date")]
        public DateTime ProtocolDate { get; set; }
        
        [Column("supplier_contact")]
        public string SupplierContact { get; set; }
        
        [Column("supplier_email")]
        public string SupplierEmail { get; set; }
        
        [Column("supplier_contact_phone")]
        public string SupplierContactPhone { get; set; }
        
        [Column("supplier_contact_fax")]
        public string SupplierContactFax { get; set; }
        
        [Column("xml")]
        public string Xml{ get; set; }
    }
}
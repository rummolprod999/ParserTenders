#region

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace ParserTenders
{
    public class Supplier
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Column("id_supplier")]
        public int Id { get; set; }

        [Column("participant_type")] public string ParticipiantType { get; set; }

        [Column("inn_supplier")] public string InnSupplier { get; set; }

        [Column("kpp_supplier")] public string KppSupplier { get; set; }

        [Column("organization_name")] public string OrganizationName { get; set; }

        [Column("country_full_name")] public string CountryFullName { get; set; }

        [Column("factual_address")] public string FactualAddress { get; set; }

        [Column("post_address")] public string PostAddress { get; set; }

        [Column("contact")] public string Contact { get; set; }

        [Column("email")] public string Email { get; set; }

        [Column("phone")] public string Phone { get; set; }

        [Column("fax")] public string Fax { get; set; }
    }
}
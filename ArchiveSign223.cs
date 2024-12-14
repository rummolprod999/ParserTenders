#region

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace ParserTenders
{
    public class ArchiveSign223
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("arhiv")] public string Archive { get; set; }

        [Column("region")] public string Region { get; set; }
    }
}
#region

using System.Data.Entity;
using MySql.Data.Entity;

#endregion

namespace ParserTenders
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class ArchiveExp223Context : DbContext
    {
        public ArchiveExp223Context()
            : base(ConnectToDb.ConnectString)
        {
        }

        public DbSet<ArchiveExp223> ArchiveExp223Results { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArchiveExp223>().ToTable(Program.TableArchiveExp223);
            base.OnModelCreating(modelBuilder);
        }
    }
}
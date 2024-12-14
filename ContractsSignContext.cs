#region

using System.Data.Entity;
using MySql.Data.Entity;

#endregion

namespace ParserTenders
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class ContractsSignContext : DbContext
    {
        public ContractsSignContext()
            : base(ConnectToDb.ConnectString)
        {
        }

        public DbSet<ContractSign> ContractsSign { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContractSign>().ToTable(Program.TableContractsSign);
            modelBuilder.Entity<Supplier>().ToTable(Program.TableSuppliers);
            base.OnModelCreating(modelBuilder);
        }
    }
}
﻿#region

using System.Data.Entity;
using MySql.Data.Entity;

#endregion

namespace ParserTenders
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class ArchiveSign223Context : DbContext
    {
        public ArchiveSign223Context()
            : base(ConnectToDb.ConnectString)
        {
        }

        public DbSet<ArchiveSign223> ArchiveSign223Results { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArchiveSign223>().ToTable(Program.TableArchiveSign223);
            base.OnModelCreating(modelBuilder);
        }
    }
}
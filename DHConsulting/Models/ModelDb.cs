using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace DHConsulting.Models
{
    public partial class ModelDb : DbContext
    {
        public ModelDb()
            : base("name=ModelDb")
        {
        }

        public virtual DbSet<Cliente> Cliente { get; set; }
        public virtual DbSet<Dettaglio> Dettaglio { get; set; }
        public virtual DbSet<Ordine> Ordine { get; set; }
        public virtual DbSet<Prodotto> Prodotto { get; set; }
        public virtual DbSet<Utente> Utente { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cliente>()
                .Property(e => e.CF)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<Cliente>()
                .Property(e => e.Piva)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<Cliente>()
                .HasMany(e => e.Ordine)
                .WithRequired(e => e.Cliente)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Ordine>()
                .HasMany(e => e.Dettaglio)
                .WithRequired(e => e.Ordine)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Prodotto>()
                .HasMany(e => e.Dettaglio)
                .WithRequired(e => e.Prodotto)
                .WillCascadeOnDelete(false);
        }
    }
}

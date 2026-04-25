using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=inventory.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique SKU constraint
            modelBuilder.Entity<Item>()
                .HasIndex(i => i.SKU)
                .IsUnique();

            // Item configuration
            modelBuilder.Entity<Item>()
                .Property(i => i.PurchasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Item>()
                .Property(i => i.SellingPrice)
                .HasPrecision(18, 2);

            // StockTransaction relationship
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Item)
                .WithMany()
                .HasForeignKey(st => st.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
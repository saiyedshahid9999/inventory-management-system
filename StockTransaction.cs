using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public virtual Item Item { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } // Add, Update, Increase, Decrease, Delete

        public int QuantityBefore { get; set; }
        public int QuantityChange { get; set; }
        public int QuantityAfter { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string PerformedBy { get; set; } = Environment.UserName;
    }
}
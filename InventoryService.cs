using System;
using System.Collections.Generic;
using System.Linq;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Services
{
    public interface IInventoryService
    {
        List<Item> GetAllItems();
        List<Item> SearchItems(string searchText);
        List<Item> GetLowStockItems();
        Item GetItemById(int id);
        bool AddItem(Item item);
        bool UpdateItem(Item item);
        bool DeleteItem(int id);
        bool AdjustStock(int itemId, int adjustment, string transactionType);
        List<StockTransaction> GetTransactionHistory(int itemId);
        DashboardStats GetDashboardStats();
    }

    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        public List<Item> GetAllItems()
        {
            return _context.Items.OrderBy(i => i.Name).ToList();
        }

        public List<Item> SearchItems(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return GetAllItems();

            var search = searchText.ToLower();
            return _context.Items
                .Where(i => i.Name.ToLower().Contains(search) ||
                           i.SKU.ToLower().Contains(search) ||
                           i.Category.ToLower().Contains(search))
                .OrderBy(i => i.Name)
                .ToList();
        }

        public List<Item> GetLowStockItems()
        {
            return _context.Items
                .Where(i => i.Quantity <= i.LowStockThreshold)
                .OrderBy(i => i.Quantity)
                .ToList();
        }

        public Item GetItemById(int id)
        {
            return _context.Items.Find(id);
        }

        public bool AddItem(Item item)
        {
            try
            {
                // Check if SKU already exists
                if (_context.Items.Any(i => i.SKU == item.SKU))
                    return false;

                item.CreatedDate = DateTime.Now;
                _context.Items.Add(item);
                _context.SaveChanges();

                // Log transaction
                LogTransaction(item.Id, "Add", 0, item.Quantity, item.Quantity, "Item created");

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateItem(Item item)
        {
            try
            {
                var existingItem = _context.Items.Find(item.Id);
                if (existingItem == null)
                    return false;

                // Check if SKU is being changed and if it conflicts
                if (existingItem.SKU != item.SKU &&
                    _context.Items.Any(i => i.SKU == item.SKU && i.Id != item.Id))
                    return false;

                var quantityBefore = existingItem.Quantity;

                existingItem.Name = item.Name;
                existingItem.SKU = item.SKU;
                existingItem.Category = item.Category;
                existingItem.PurchasePrice = item.PurchasePrice;
                existingItem.SellingPrice = item.SellingPrice;
                existingItem.Quantity = item.Quantity;
                existingItem.LowStockThreshold = item.LowStockThreshold;
                existingItem.LastModifiedDate = DateTime.Now;

                _context.SaveChanges();

                // Log if quantity changed
                if (quantityBefore != item.Quantity)
                {
                    var change = item.Quantity - quantityBefore;
                    LogTransaction(item.Id, "Update", quantityBefore, change, item.Quantity, "Item updated");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteItem(int id)
        {
            try
            {
                var item = _context.Items.Find(id);
                if (item == null)
                    return false;

                LogTransaction(id, "Delete", item.Quantity, -item.Quantity, 0, "Item deleted");

                _context.Items.Remove(item);
                _context.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AdjustStock(int itemId, int adjustment, string transactionType)
        {
            try
            {
                var item = _context.Items.Find(itemId);
                if (item == null)
                    return false;

                var quantityBefore = item.Quantity;
                item.Quantity += adjustment;

                // Don't allow negative stock
                if (item.Quantity < 0)
                    item.Quantity = 0;

                item.LastModifiedDate = DateTime.Now;
                _context.SaveChanges();

                var actualChange = item.Quantity - quantityBefore;
                LogTransaction(itemId, transactionType, quantityBefore, actualChange, item.Quantity,
                    $"Stock adjusted by {adjustment}");

                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<StockTransaction> GetTransactionHistory(int itemId)
        {
            return _context.StockTransactions
                .Where(t => t.ItemId == itemId)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }

        public DashboardStats GetDashboardStats()
        {
            var items = _context.Items.ToList();

            return new DashboardStats
            {
                TotalItems = items.Count,
                TotalInventoryValue = items.Sum(i => i.TotalValue),
                TotalPotentialProfit = items.Sum(i => i.PotentialProfit),
                LowStockCount = items.Count(i => i.IsLowStock),
                TotalCategories = items.Select(i => i.Category).Distinct().Count(),
                AverageStockLevel = items.Any() ? items.Average(i => i.Quantity) : 0
            };
        }

        private void LogTransaction(int itemId, string type, int quantityBefore, int change, int quantityAfter, string notes)
        {
            var transaction = new StockTransaction
            {
                ItemId = itemId,
                TransactionType = type,
                QuantityBefore = quantityBefore,
                QuantityChange = change,
                QuantityAfter = quantityAfter,
                Notes = notes,
                TransactionDate = DateTime.Now
            };

            _context.StockTransactions.Add(transaction);
            _context.SaveChanges();
        }
    }

    public class DashboardStats
    {
        public int TotalItems { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal TotalPotentialProfit { get; set; }
        public int LowStockCount { get; set; }
        public int TotalCategories { get; set; }
        public double AverageStockLevel { get; set; }
    }
}
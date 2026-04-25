using InventoryManagementSystem.Commands;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace InventoryManagementSystem.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IInventoryService _inventoryService;
        private readonly ExportService _exportService;

        #region Properties

        private ObservableCollection<Item> _items;
        public ObservableCollection<Item> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
            }
        }

        private string _itemName;
        public string ItemName
        {
            get => _itemName;
            set
            {
                _itemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }

        private string _sku;
        public string SKU
        {
            get => _sku;
            set
            {
                _sku = value;
                OnPropertyChanged(nameof(SKU));
            }
        }

        private string _category;
        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        private decimal _purchasePrice;
        public decimal PurchasePrice
        {
            get => _purchasePrice;
            set
            {
                _purchasePrice = value;
                OnPropertyChanged(nameof(PurchasePrice));
            }
        }

        private decimal _sellingPrice;
        public decimal SellingPrice
        {
            get => _sellingPrice;
            set
            {
                _sellingPrice = value;
                OnPropertyChanged(nameof(SellingPrice));
            }
        }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        private int _lowStockThreshold;
        public int LowStockThreshold
        {
            get => _lowStockThreshold;
            set
            {
                _lowStockThreshold = value;
                OnPropertyChanged(nameof(LowStockThreshold));
            }
        }

        private int _adjustmentAmount;
        public int AdjustmentAmount
        {
            get => _adjustmentAmount;
            set
            {
                _adjustmentAmount = value;
                OnPropertyChanged(nameof(AdjustmentAmount));
            }
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilterItems();
            }
        }

        private Item _selectedItem;
        public Item SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));

                if (_selectedItem != null)
                {
                    PopulateFieldsFromSelectedItem();
                }
            }
        }

        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged(nameof(TotalItems));
            }
        }

        private decimal _totalInventoryValue;
        public decimal TotalInventoryValue
        {
            get => _totalInventoryValue;
            set
            {
                _totalInventoryValue = value;
                OnPropertyChanged(nameof(TotalInventoryValue));
            }
        }

        private decimal _totalPotentialProfit;
        public decimal TotalPotentialProfit
        {
            get => _totalPotentialProfit;
            set
            {
                _totalPotentialProfit = value;
                OnPropertyChanged(nameof(TotalPotentialProfit));
            }
        }

        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set
            {
                _lowStockCount = value;
                OnPropertyChanged(nameof(LowStockCount));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        #endregion

        #region Commands

        public RelayCommand AddCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand UpdateCommand { get; set; }
        public RelayCommand IncreaseStockCommand { get; set; }
        public RelayCommand DecreaseStockCommand { get; set; }
        public RelayCommand ShowLowStockCommand { get; set; }
        public RelayCommand ShowAllCommand { get; set; }
        public RelayCommand ExportExcelCommand { get; set; }
        public RelayCommand ResetFormCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }

        #endregion

        public MainViewModel()
        {
            var context = new AppDbContext();
            context.Database.EnsureCreated();

            _inventoryService = new InventoryService(context);
            _exportService = new ExportService();

            Items = new ObservableCollection<Item>();

            InitializeCommands();
            LoadAllItems();
            UpdateDashboardStats();
        }

        private void InitializeCommands()
        {
            AddCommand = new RelayCommand(AddItem, CanAddItem);
            DeleteCommand = new RelayCommand(DeleteItem, () => SelectedItem != null);
            UpdateCommand = new RelayCommand(UpdateItem, () => SelectedItem != null && CanAddItem());
            IncreaseStockCommand = new RelayCommand(IncreaseStock, () => SelectedItem != null && AdjustmentAmount > 0);
            DecreaseStockCommand = new RelayCommand(DecreaseStock, () => SelectedItem != null && AdjustmentAmount > 0);
            ShowLowStockCommand = new RelayCommand(ShowLowStock);
            ShowAllCommand = new RelayCommand(LoadAllItems);
            ExportExcelCommand = new RelayCommand(ExportToExcel, () => Items?.Count > 0);
            ResetFormCommand = new RelayCommand(ResetFields);
            RefreshCommand = new RelayCommand(LoadAllItems);
        }

        private bool CanAddItem()
        {
            return !string.IsNullOrWhiteSpace(ItemName) &&
                   !string.IsNullOrWhiteSpace(SKU) &&
                   PurchasePrice >= 0 &&
                   SellingPrice >= 0 &&
                   Quantity >= 0 &&
                   LowStockThreshold >= 0;
        }

        private void AddItem()
        {
            try
            {
                IsLoading = true;

                var item = new Item
                {
                    Name = ItemName.Trim(),
                    SKU = SKU.Trim(),
                    Category = Category?.Trim(),
                    PurchasePrice = PurchasePrice,
                    SellingPrice = SellingPrice,
                    Quantity = Quantity,
                    LowStockThreshold = LowStockThreshold
                };

                if (_inventoryService.AddItem(item))
                {
                    LoadAllItems();
                    ResetFields();
                    ShowSuccessMessage("Item added successfully!");
                }
                else
                {
                    ShowErrorMessage("Failed to add item. SKU might already exist.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error adding item: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void DeleteItem()
        {
            if (SelectedItem == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete '{SelectedItem.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsLoading = true;

                if (_inventoryService.DeleteItem(SelectedItem.Id))
                {
                    LoadAllItems();
                    ResetFields();
                    ShowSuccessMessage("Item deleted successfully!");
                }
                else
                {
                    ShowErrorMessage("Failed to delete item.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error deleting item: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateItem()
        {
            if (SelectedItem == null)
            {
                ShowErrorMessage("Please select an item to update.");
                return;
            }

            try
            {
                IsLoading = true;

                var updatedItem = new Item
                {
                    Id = SelectedItem.Id,
                    Name = ItemName.Trim(),
                    SKU = SKU.Trim(),
                    Category = Category?.Trim(),
                    PurchasePrice = PurchasePrice,
                    SellingPrice = SellingPrice,
                    Quantity = Quantity,
                    LowStockThreshold = LowStockThreshold
                };

                if (_inventoryService.UpdateItem(updatedItem))
                {
                    LoadAllItems();
                    ShowSuccessMessage("Item updated successfully!");
                }
                else
                {
                    ShowErrorMessage("Failed to update item. SKU might already exist.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error updating item: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void IncreaseStock()
        {
            if (SelectedItem == null || AdjustmentAmount <= 0) return;

            try
            {
                IsLoading = true;

                if (_inventoryService.AdjustStock(SelectedItem.Id, AdjustmentAmount, "Increase"))
                {
                    LoadAllItems();
                    ShowSuccessMessage($"Stock increased by {AdjustmentAmount}");
                }
                else
                {
                    ShowErrorMessage("Failed to increase stock.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error increasing stock: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void DecreaseStock()
        {
            if (SelectedItem == null || AdjustmentAmount <= 0) return;

            try
            {
                IsLoading = true;

                if (_inventoryService.AdjustStock(SelectedItem.Id, -AdjustmentAmount, "Decrease"))
                {
                    LoadAllItems();
                    ShowSuccessMessage($"Stock decreased by {AdjustmentAmount}");
                }
                else
                {
                    ShowErrorMessage("Failed to decrease stock.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error decreasing stock: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowLowStock()
        {
            try
            {
                IsLoading = true;

                var lowStockItems = _inventoryService.GetLowStockItems();
                Items.Clear();
                foreach (var item in lowStockItems)
                    Items.Add(item);

                UpdateDashboardStats();

                if (lowStockItems.Count == 0)
                {
                    MessageBox.Show("No low stock items found!", "Low Stock Report",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading low stock items: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadAllItems()
        {
            try
            {
                IsLoading = true;

                var allItems = _inventoryService.GetAllItems();
                Items.Clear();
                foreach (var item in allItems)
                    Items.Add(item);

                UpdateDashboardStats();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error loading items: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterItems()
        {
            try
            {
                var filtered = _inventoryService.SearchItems(SearchText);
                Items.Clear();
                foreach (var item in filtered)
                    Items.Add(item);

                UpdateDashboardStats();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error filtering items: {ex.Message}");
            }
        }

        private void ExportToExcel()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Export Inventory Report",
                    FileName = $"Inventory_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                IsLoading = true;

                if (_exportService.ExportToExcel(Items.ToList(), saveFileDialog.FileName))
                {
                    var result = MessageBox.Show(
                        "Export successful! Would you like to open the file?",
                        "Export Complete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    ShowErrorMessage("Failed to export to Excel.");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error exporting to Excel: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ResetFields()
        {
            ItemName = string.Empty;
            SKU = string.Empty;
            Category = string.Empty;
            PurchasePrice = 0;
            SellingPrice = 0;
            Quantity = 0;
            LowStockThreshold = 0;
            AdjustmentAmount = 0;
            SelectedItem = null;
        }

        private void PopulateFieldsFromSelectedItem()
        {
            if (SelectedItem == null) return;

            ItemName = SelectedItem.Name;
            SKU = SelectedItem.SKU;
            Category = SelectedItem.Category;
            PurchasePrice = SelectedItem.PurchasePrice;
            SellingPrice = SelectedItem.SellingPrice;
            Quantity = SelectedItem.Quantity;
            LowStockThreshold = SelectedItem.LowStockThreshold;
        }

        private void UpdateDashboardStats()
        {
            try
            {
                var stats = _inventoryService.GetDashboardStats();
                TotalItems = stats.TotalItems;
                TotalInventoryValue = stats.TotalInventoryValue;
                TotalPotentialProfit = stats.TotalPotentialProfit;
                LowStockCount = stats.LowStockCount;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error updating dashboard: {ex.Message}");
            }
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
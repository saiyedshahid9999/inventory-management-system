using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services
{
    public interface IExportService
    {
        bool ExportToExcel(List<Item> items, string filePath);
    }

    public class ExportService : IExportService
    {
        public bool ExportToExcel(List<Item> items, string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Inventory Report");

                    worksheet.Cell(1, 1).Value = "INVENTORY REPORT";
                    worksheet.Range("A1:J1").Merge();
                    worksheet.Cell(1, 1).Style.Font.Bold = true;

                    int row = 3;

                    worksheet.Cell(row, 1).Value = "SKU";
                    worksheet.Cell(row, 2).Value = "Name";
                    worksheet.Cell(row, 3).Value = "Category";
                    worksheet.Cell(row, 4).Value = "Purchase Price";
                    worksheet.Cell(row, 5).Value = "Selling Price";
                    worksheet.Cell(row, 6).Value = "Quantity";
                    worksheet.Cell(row, 7).Value = "Threshold";

                    var header = worksheet.Range($"A{row}:G{row}");
                    header.Style.Font.Bold = true;

                    row++;

                    foreach (var item in items)
                    {
                        worksheet.Cell(row, 1).Value = item.SKU;
                        worksheet.Cell(row, 2).Value = item.Name;
                        worksheet.Cell(row, 3).Value = item.Category;
                        worksheet.Cell(row, 4).Value = item.PurchasePrice;
                        worksheet.Cell(row, 5).Value = item.SellingPrice;
                        worksheet.Cell(row, 6).Value = item.Quantity;
                        worksheet.Cell(row, 7).Value = item.LowStockThreshold;

                        if (item.IsLowStock)
                        {
                            worksheet.Range($"A{row}:G{row}")
                                .Style.Fill.BackgroundColor = XLColor.LightPink;
                        }

                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(filePath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
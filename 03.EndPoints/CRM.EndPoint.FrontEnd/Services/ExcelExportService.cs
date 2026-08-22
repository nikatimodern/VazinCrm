using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Services;

public class ExcelColumnConfig<T> {
    public string Header { get; init; }
    public Func<T, object> ValueSelector { get; init; }
    public string NumberFormat { get; init; } // مثال: "#,##0_-ريال"
}

public class ExcelExportService {
    public static FileContentResult ExportToExcel<T>(
        IEnumerable<T> data,
        List<ExcelColumnConfig<T>> columns,
        string fileName = "Export",
        bool rightToLeft = true,
        bool includeRowNumber = true,
        bool convertToTable = true
    )
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(fileName);
        worksheet.RightToLeft = rightToLeft;

        var currentRow = 1;

        var allHeaders = new List<string>();
        if (includeRowNumber)
        {
            allHeaders.Add("ردیف");
        }

        allHeaders.AddRange(columns.Select(c => c.Header));

        for (var i = 0; i < allHeaders.Count; i++)
        {
            worksheet.Cell(currentRow, i + 1).Value = allHeaders[i];
        }

        var rowData = data?.ToList() ?? [];
        for (var rowIdx = 0; rowIdx < rowData.Count; rowIdx++)
        {
            currentRow++;
            var item = rowData[rowIdx];
            var currentCol = 1;

            if (includeRowNumber)
            {
                worksheet.Cell(currentRow, currentCol).Value = rowIdx + 1;
                currentCol++;
            }

            foreach (var colConfig in columns)
            {
                var val = colConfig.ValueSelector(item);
                worksheet.Cell(currentRow, currentCol).Value = XLCellValue.FromObject(val);

                if (!string.IsNullOrEmpty(colConfig.NumberFormat))
                {
                    worksheet.Cell(currentRow, currentCol).Style.NumberFormat.Format = colConfig.NumberFormat;
                }

                currentCol++;
            }

            worksheet.Row(currentRow).Height = 22;
        }

        var dataRange = worksheet.Range(1, 1, currentRow, allHeaders.Count);

        if (convertToTable)
        {
            var table = dataRange.CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium1;
        } else
        {
            var headerColor = XLColor.FromHtml("#D9E2F3");
            var zebraColor = XLColor.FromHtml("#F6F8FC");

            var headerRow = worksheet.Row(1);
            headerRow.Style.Fill.SetBackgroundColor(headerColor);
            headerRow.Style.Font.Bold = true;

            for (var r = 2; r <= currentRow; r++)
            {
                if (r % 2 == 0)
                {
                    worksheet.Row(r).Style.Fill.SetBackgroundColor(zebraColor);
                }
            }
        }

        worksheet.Row(1).Height = 26;
        worksheet.Row(1).Style.Font.Bold = true;


        worksheet.Columns().AdjustToContents();
        foreach (var col in worksheet.ColumnsUsed())
        {
            col.Width += 4;
        }

        worksheet.Columns().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Columns().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;


        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var downloadFileName = fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}.xlsx";

        return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet") {
            FileDownloadName = downloadFileName
        };
    }
}
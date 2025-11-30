using System;
using ClosedXML.Excel;
using InvoiceMaker.Models;

namespace InvoiceMaker.Services
{
    public class ExcelExportService
    {
        private readonly string _templatePath;

        public ExcelExportService(string templatePath)
        {
            _templatePath = templatePath;
        }
        public void Export(Invoice invoice, string outputPath)
        {
            using (var workbook = new XLWorkbook(_templatePath))
            {
                var ws = workbook.Worksheet(1);

                // 헤더
                ws.Cell("I9").Value = invoice.InvoiceDate;
                ws.Cell("C8").Value = invoice.ClientName ?? "";
                ws.Cell("I10").Value = invoice.ExchangeRate;

                // 항목 위치
                int topRow = 12;     // 윗 인보이스 시작
                int bottomRow = 34;  // 아랫 인보이스 시작

                foreach (var item in invoice.Items)
                {
                    // 완전 빈 항목 스킵
                    if (item.Days == 0 &&
                        item.Quantity == 0 &&
                        item.UnitPrice == 0 &&
                        string.IsNullOrWhiteSpace(item.Description))
                        continue;

                    // =======================
                    // 1) 윗 인보이스 직접 값 입력
                    // =======================
                    ws.Cell($"A{topRow}").Value = item.StartDate;
                    ws.Cell($"C{topRow}").Value = item.EndDate;
                    ws.Cell($"D{topRow}").Value = item.RoomNumber ?? "";

                    var desc = string.IsNullOrWhiteSpace(item.Description)
                        ? item.ItemType
                        : item.Description;
                    ws.Cell($"E{topRow}").Value = desc;

                    ws.Cell($"F{topRow}").Value = item.UnitPrice;
                    ws.Cell($"G{topRow}").Value = item.Quantity;
                    ws.Cell($"H{topRow}").Value = item.Days;

                    // 금액 USD
                    ws.Cell($"I{topRow}").FormulaA1 = $"=F{topRow}*G{topRow}*H{topRow}";

                    // 금액 PESO
                    ws.Cell($"J{topRow}").FormulaA1 = $"=I{topRow}*$I$10";

                    // =======================
                    // 2) 아랫 인보이스 = 윗 인보이스 참조
                    // =======================

                    ws.Cell($"A{bottomRow}").FormulaA1 = $"=A{topRow}";
                    ws.Cell($"B{bottomRow}").FormulaA1 = $"=B{topRow}";
                    ws.Cell($"C{bottomRow}").FormulaA1 = $"=C{topRow}";
                    ws.Cell($"D{bottomRow}").FormulaA1 = $"=D{topRow}";
                    ws.Cell($"F{bottomRow}").FormulaA1 = $"=F{topRow}";
                    ws.Cell($"G{bottomRow}").FormulaA1 = $"=G{topRow}";
                    ws.Cell($"H{bottomRow}").FormulaA1 = $"=H{topRow}";
                    ws.Cell($"I{bottomRow}").FormulaA1 = $"=I{topRow}";
                    ws.Cell($"J{bottomRow}").FormulaA1 = $"=J{topRow}";

                    topRow++;
                    bottomRow++;
                }

                workbook.SaveAs(outputPath);
            }
        }

    }
}


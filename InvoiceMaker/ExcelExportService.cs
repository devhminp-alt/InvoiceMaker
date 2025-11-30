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
                ws.Cell("I9").Value = invoice.InvoiceDate;               // Fecha de la factura
                ws.Cell("C8").Value = invoice.ClientName ?? string.Empty;
                ws.Cell("I10").Value = invoice.ExchangeRate;              // 1 USD = MXN (환율)

                int topRow = 12;  // 위 인보이스 시작
                int bottomRow = 34;  // 아래 인보이스 시작

                foreach (var item in invoice.Items)
                {
                    // 완전 빈 항목 스킵
                    if (item.Days == 0 &&
                        item.Quantity == 0 &&
                        item.UnitPrice == 0 &&
                        string.IsNullOrWhiteSpace(item.Description) &&
                        string.IsNullOrWhiteSpace(item.ItemType))
                    {
                        continue;
                    }

                    // ===== 위 인보이스 (직접 값 입력) =====
                    ws.Cell($"A{topRow}").Value = item.StartDate;
                    ws.Cell($"C{topRow}").Value = item.EndDate;
                    ws.Cell($"D{topRow}").Value = item.RoomNumber ?? string.Empty;

                    var desc = string.IsNullOrWhiteSpace(item.Description)
                        ? item.ItemType
                        : item.Description;
                    ws.Cell($"E{topRow}").Value = desc ?? string.Empty;

                    ws.Cell($"F{topRow}").Value = item.UnitPrice;
                    ws.Cell($"G{topRow}").Value = item.Quantity;
                    ws.Cell($"H{topRow}").Value = item.Days;

                    // I: USD 금액 (엑셀 수식)
                    ws.Cell($"I{topRow}").FormulaA1 = $"=F{topRow}*G{topRow}*H{topRow}";
                    // J: Peso 금액
                    ws.Cell($"J{topRow}").FormulaA1 = $"=I{topRow}*$I$10";

                    // ===== 아래 인보이스 (위 셀 참조) =====
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

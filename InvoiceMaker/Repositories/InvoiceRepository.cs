using System.Data.SQLite;
using InvoiceMaker.Data;

namespace InvoiceMaker.Repositories
{
    public class InvoiceRepository
    {
        public long CreateInvoice(long reservationId,
                                  string invoiceNo,
                                  decimal exchangeRate,
                                  decimal totalAmount)
        {
            using var conn = DbContext.GetConnection();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Invoice
                (InvoiceNo, ReservationId, InvoiceDate, Currency,
                 ExchangeRate, TotalAmount, Status)
                VALUES
                (@no, @rid, DATE('now'), 'KRW',
                 @rate, @total, 'Issued');
                SELECT last_insert_rowid();
            ", conn);

            cmd.Parameters.AddWithValue("@no", invoiceNo);
            cmd.Parameters.AddWithValue("@rid", reservationId);
            cmd.Parameters.AddWithValue("@rate", exchangeRate);
            cmd.Parameters.AddWithValue("@total", totalAmount);

            return (long)cmd.ExecuteScalar();
        }
    }
}

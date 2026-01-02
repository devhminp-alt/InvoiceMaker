using System.Data.SQLite;
using InvoiceMaker.Data;
using InvoiceMaker.Models;

namespace InvoiceMaker.Repositories
{
    public class InvoiceItemRepository
    {
        public void Insert(long invoiceId, InvoiceItem item)
        {
            using var conn = DbContext.GetConnection();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO InvoiceItem
                (InvoiceId, ItemType, Description,
                 UnitPrice, Quantity, Amount)
                VALUES
                (@iid, @type, @desc,
                 @price, @qty, @amt)
            ", conn);

            cmd.Parameters.AddWithValue("@iid", invoiceId);
            cmd.Parameters.AddWithValue("@type", item.ItemType);
            cmd.Parameters.AddWithValue("@desc", item.Description);
            cmd.Parameters.AddWithValue("@price", item.UnitPrice);
            cmd.Parameters.AddWithValue("@qty", item.Quantity);

            cmd.ExecuteNonQuery();
        }
    }
}

using System.Collections.Generic;
using System.Data.SQLite;
using InvoiceMaker.Data;
using InvoiceMaker.Models;

namespace InvoiceMaker.Repositories
{
    public class ItemMasterRepository
    {
        public List<ItemMaster> GetAll()
        {
            var list = new List<ItemMaster>();

            using var conn = DbContext.GetConnection();
            using var cmd = new SQLiteCommand(@"
                SELECT
                    Id,
                    ItemName,
                    UnitPrice,
                    Description
                FROM ItemMaster
                ORDER BY Id
            ", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ItemMaster
                {
                    Id = reader.GetInt32(0),
                    ItemName = reader.GetString(1),
                    UnitPrice = reader.GetDecimal(2),
                    Description = reader.GetString(3)
                });
            }

            return list;
        }
    }
}

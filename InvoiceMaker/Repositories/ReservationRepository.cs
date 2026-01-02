using System;
using System.Collections.Generic;
using System.Data.SQLite;
using InvoiceMaker.Data;
using InvoiceMaker.Models;

namespace InvoiceMaker.Repositories
{
    public class ReservationRepository
    {
        public List<Reservation> GetInvoiceableReservations()
        {
            var list = new List<Reservation>();

            using var conn = DbContext.GetConnection();
            using var cmd = new SQLiteCommand(@"
                SELECT
                    r.ReservationId,
                    rm.RoomName,
                    r.CheckInDate,
                    r.CheckOutDate
                FROM Reservation r
                JOIN Room rm ON r.RoomId = rm.RoomId
                WHERE r.Status IN ('Reserved','Checked-In')
                  AND NOT EXISTS (
                    SELECT 1 FROM Invoice i
                    WHERE i.ReservationId = r.ReservationId
                  )
            ", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Reservation
                {
                    ReservationId = reader.GetInt64(0),
                    RoomName = reader.GetString(1),
                    CheckInDate = DateTime.Parse(reader.GetString(2)),
                    CheckOutDate = DateTime.Parse(reader.GetString(3))
                });
            }

            return list;
        }
    }
}

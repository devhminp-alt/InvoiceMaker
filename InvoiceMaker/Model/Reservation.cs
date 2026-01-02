using System;

namespace InvoiceMaker.Models
{
    public class Reservation
    {
        public long ReservationId
        {
            get; set;
        }
        public string RoomName
        {
            get; set;
        }
        public DateTime CheckInDate
        {
            get; set;
        }
        public DateTime CheckOutDate
        {
            get; set;
        }

        public int Nights =>
            (CheckOutDate - CheckInDate).Days;
    }
}

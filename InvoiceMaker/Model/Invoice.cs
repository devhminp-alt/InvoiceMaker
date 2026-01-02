using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace InvoiceMaker.Models
{
    public class Invoice
    {
        public Invoice()
        {
            InvoiceDate = DateTime.Today;
            ClientName = string.Empty;
            Items = new ObservableCollection<InvoiceItem>();
        }

        public DateTime InvoiceDate
        {
            get; set;
        }

        public string ClientName
        {
            get; set;
        }

        /// <summary>
        /// 1 USD = ? MXN (엑셀 템플릿용)
        /// </summary>
        public decimal ExchangeRate
        {
            get; set;
        }

        public ObservableCollection<InvoiceItem> Items
        {
            get;
        }

        public decimal TotalUsd => Items.Sum(i => i.AmountUsd);
        public decimal TotalPeso => Items.Sum(i => i.AmountPeso);
        public decimal TotalKrw => Items.Sum(i => i.AmountKrw);

        public long InvoiceId
        {
            get; set;
        }
        public long ReservationId
        {
            get; set;
        }
        public string InvoiceNo
        {
            get; set;
        }

    }
}

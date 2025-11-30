using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace InvoiceMaker.Models
{
    public class Invoice
    {
        public Invoice()
        {
            Items = new ObservableCollection<InvoiceItem>();
            InvoiceDate = DateTime.Today;
            Currency = "USD";          // 기준 통화
        }

        public DateTime InvoiceDate
        {
            get; set;
        }

        public string ClientName
        {
            get; set;
        }

        // 표시용 통화 코드 (USD)
        public string Currency
        {
            get; set;
        }

        // 1 USD = ? MXN (PESO)
        public decimal ExchangeRate
        {
            get; set;
        }

        public ObservableCollection<InvoiceItem> Items
        {
            get; set;
        }

        // 합계 (USD) = 각 항목 AmountUsd 합
        public decimal TotalUsd
        {
            get
            {
                return Items.Sum(i => i.AmountUsd);
            }
        }

        // 합계 (PESO) = 각 항목 AmountPeso 합
        public decimal TotalPeso
        {
            get
            {
                return Items.Sum(i => i.AmountPeso);
            }
        }
    }
}

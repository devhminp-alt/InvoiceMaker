using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoiceMaker.Models
{
    public class InvoiceItem : INotifyPropertyChanged
    {
        public InvoiceItem()
        {
            _itemType = "숙박";
            _description = "";
            _quantity = 1;    // 인원수
            _unitPrice = 0m;   // 단가(USD)
            _roomNumber = "";
            _exchangeRate = 0m;   // 1 USD = ? MXN
            _startDate = null;
            _endDate = null;
        }

        private string _itemType;
        private string _description;
        private int _quantity;
        private decimal _unitPrice;     // USD
        private string _roomNumber;
        private decimal _exchangeRate;  // USD -> MXN
        private DateTime? _startDate;
        private DateTime? _endDate;

        public string ItemType
        {
            get
            {
                return _itemType;
            }
            set
            {
                if (_itemType != value)
                {
                    _itemType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        // 인원수
        public int Quantity
        {
            get
            {
                return _quantity;
            }
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged("AmountUsd");
                    OnPropertyChanged("AmountPeso");
                }
            }
        }

        // 단가(USD)
        public decimal UnitPrice
        {
            get
            {
                return _unitPrice;
            }
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged("AmountUsd");
                    OnPropertyChanged("AmountPeso");
                }
            }
        }

        // 방 번호 (숙박 시 사용)
        public string RoomNumber
        {
            get
            {
                return _roomNumber;
            }
            set
            {
                if (_roomNumber != value)
                {
                    _roomNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        // 1 USD = ? MXN
        public decimal ExchangeRate
        {
            get
            {
                return _exchangeRate;
            }
            set
            {
                if (_exchangeRate != value)
                {
                    _exchangeRate = value;
                    OnPropertyChanged();
                    OnPropertyChanged("AmountPeso");
                }
            }
        }

        // 시작일
        public DateTime? StartDate
        {
            get
            {
                return _startDate;
            }
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Days");
                    OnPropertyChanged("AmountUsd");
                    OnPropertyChanged("AmountPeso");
                }
            }
        }

        // 종료일
        public DateTime? EndDate
        {
            get
            {
                return _endDate;
            }
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged("Days");
                    OnPropertyChanged("AmountUsd");
                    OnPropertyChanged("AmountPeso");
                }
            }
        }

        // dias = (EndDate - StartDate) + 1  (둘 중 하나라도 없으면 0)
        public int Days
        {
            get
            {
                if (!_startDate.HasValue || !_endDate.HasValue)
                    return 0;

                var s = _startDate.Value.Date;
                var e = _endDate.Value.Date;
                if (e < s) e = s;

                return (e - s).Days + 1;
            }
        }

        // 금액(USD) = dias * 인원수 * 단가(USD)
        public decimal AmountUsd
        {
            get
            {
                return Days * _quantity * _unitPrice;
            }
        }

        // 금액(PESO) = 금액(USD) * 환율
        public decimal AmountPeso
        {
            get
            {
                return AmountUsd * _exchangeRate;
            }
        }

        // ===== INotifyPropertyChanged =====

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}

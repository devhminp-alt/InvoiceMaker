using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoiceMaker.Models
{
    public class InvoiceItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public InvoiceItem()
        {
            _itemType = "숙박";
            _description = string.Empty;
            _quantity = 1;
            _unitPrice = 0m;
            _discountPercent = 0m;
        }

        // ===== 백킹 필드 =====
        private string _itemType;
        private string _description;
        private string _roomNumber;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _discountPercent;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private decimal _exchangeRate;     // USD -> MXN
        private decimal _exchangeRateKrw;  // USD -> KRW

        // ===== 프로퍼티 =====

        /// <summary>항목 타입 (숙박/출퇴근/공항픽업/오마카세/주말식사)</summary>
        public string ItemType
        {
            get => _itemType;
            set
            {
                if (_itemType != value)
                {
                    _itemType = value;
                    OnPropertyChanged(); // *** 이게 있어야 ViewModel에서 단가/날짜 갱신됨 ***
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RoomNumber
        {
            get => _roomNumber;
            set
            {
                if (_roomNumber != value)
                {
                    _roomNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>인원수</summary>
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AmountUsd));
                    OnPropertyChanged(nameof(AmountPeso));
                    OnPropertyChanged(nameof(AmountKrw));
                }
            }
        }

        /// <summary>단가(USD)</summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AmountUsd));
                    OnPropertyChanged(nameof(AmountPeso));
                    OnPropertyChanged(nameof(AmountKrw));
                }
            }
        }

        /// <summary>할인율(%)</summary>
        public decimal DiscountPercent
        {
            get => _discountPercent;
            set
            {
                if (_discountPercent != value)
                {
                    _discountPercent = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AmountUsd));
                    OnPropertyChanged(nameof(AmountPeso));
                    OnPropertyChanged(nameof(AmountKrw));
                }
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (_startDate != value)
                {
                    _startDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Days));
                    OnPropertyChanged(nameof(AmountUsd));
                    OnPropertyChanged(nameof(AmountPeso));
                    OnPropertyChanged(nameof(AmountKrw));
                }
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Days));
                    OnPropertyChanged(nameof(AmountUsd));
                    OnPropertyChanged(nameof(AmountPeso));
                    OnPropertyChanged(nameof(AmountKrw));
                }
            }
        }

        /// <summary>1 USD = ? MXN</summary>
        public decimal ExchangeRate
        {
            get => _exchangeRate;
            set
            {
                if (_exchangeRate != value)
                {
                    _exchangeRate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AmountPeso));
                }
            }
        }

        /// <summary>1 USD = ? KRW</summary>
        public decimal ExchangeRateKrw
        {
            get => _exchangeRateKrw;
            set
            {
                if (_exchangeRateKrw != value)
                {
                    _exchangeRateKrw = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AmountKrw));
                }
            }
        }

        // ===== 계산 프로퍼티 =====

        /// <summary>dias (종료일 - 시작일 + 1). 날짜 없으면 0</summary>
        public int Days
        {
            get
            {
                if (!StartDate.HasValue || !EndDate.HasValue)
                    return 0;

                var s = StartDate.Value.Date;
                var e = EndDate.Value.Date;
                if (e < s) e = s;

                return (e - s).Days + 1;
            }
        }

        /// <summary>금액(USD) = 단가 * 인원수 * dias * (1 - 할인율)</summary>
        public decimal AmountUsd
        {
            get
            {
                var baseAmount = UnitPrice * Quantity * Days;
                var discountFactor = 1m - (DiscountPercent / 100m);
                if (discountFactor < 0) discountFactor = 0;
                return Math.Round(baseAmount * discountFactor, 2);
            }
        }

        /// <summary>금액(PESO) = AmountUsd * 환율(USD→MXN)</summary>
        public decimal AmountPeso => Math.Round(AmountUsd * ExchangeRate, 2);

        /// <summary>금액(원화) = AmountUsd * 환율(USD→KRW)</summary>
        public decimal AmountKrw => Math.Round(AmountUsd * ExchangeRateKrw, 0);
    }
}

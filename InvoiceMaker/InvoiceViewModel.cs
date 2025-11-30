using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using InvoiceMaker.Models;
using InvoiceMaker.Services;

namespace InvoiceMaker.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        private readonly ExchangeRateService _exchangeRateService;

        private bool _isLoadingRate;
        private decimal _exchangeRateUsdToKrw;
        private decimal _globalDiscountPercent;
        private DateTime? _lodgingStartDate;
        private DateTime? _lodgingEndDate;

        public InvoiceViewModel()
        {
            _exchangeRateService = new ExchangeRateService();

            Invoice = new Invoice();
            Items = Invoice.Items;

            // 기본 날짜
            LodgingStartDate = DateTime.Today;
            LodgingEndDate = DateTime.Today.AddDays(1);

            // 항목 타입 리스트
            ItemTypes = new ObservableCollection<string>
            {
                "숙박",
                "출퇴근",
                "공항픽업",
                "오마카세",
                "주말식사"
            };

            // 커맨드
            RefreshRateCommand = new RelayCommand(async _ => await LoadExchangeRateAsync());
            ExportToExcelCommand = new RelayCommand(_ => ExportToExcel());
            AddItemCommand = new RelayCommand(_ => AddNewItem());
            RemoveItemCommand = new RelayCommand(p => RemoveItem(p as InvoiceItem));

            // 초기에 기본 항목 1개 (숙박)
            AddInitialItem("숙박");

            // 컬렉션 변경 이벤트
            Items.CollectionChanged += Items_CollectionChanged;

            // 시작 시 환율 로딩
            Task.Run(async () => await LoadExchangeRateAsync());
        }

        // ===== 프로퍼티 =====

        public Invoice Invoice
        {
            get;
        }

        public ObservableCollection<InvoiceItem> Items
        {
            get;
        }

        public ObservableCollection<string> ItemTypes
        {
            get;
        }

        public bool IsLoadingRate
        {
            get => _isLoadingRate;
            set
            {
                if (_isLoadingRate != value)
                {
                    _isLoadingRate = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>1 USD = ? MXN</summary>
        public decimal ExchangeRateUsdToMxn
        {
            get => Invoice.ExchangeRate;
            set
            {
                if (Invoice.ExchangeRate != value)
                {
                    Invoice.ExchangeRate = value;
                    OnPropertyChanged();
                    UpdateItemExchangeRates();
                }
            }
        }

        /// <summary>1 USD = ? KRW</summary>
        public decimal ExchangeRateUsdToKrw
        {
            get => _exchangeRateUsdToKrw;
            set
            {
                if (_exchangeRateUsdToKrw != value)
                {
                    _exchangeRateUsdToKrw = value;
                    OnPropertyChanged();
                    UpdateItemExchangeRates();
                }
            }
        }

        /// <summary>전체 할인율 (%)</summary>
        public decimal GlobalDiscountPercent
        {
            get => _globalDiscountPercent;
            set
            {
                if (_globalDiscountPercent != value)
                {
                    _globalDiscountPercent = value;
                    OnPropertyChanged();

                    foreach (var item in Items)
                    {
                        item.DiscountPercent = value;
                    }

                    RecalculateTotals();
                }
            }
        }

        public DateTime? LodgingStartDate
        {
            get => _lodgingStartDate;
            set
            {
                if (_lodgingStartDate != value)
                {
                    _lodgingStartDate = value;
                    OnPropertyChanged();
                    ApplyHeaderDatesToItems();
                }
            }
        }

        public DateTime? LodgingEndDate
        {
            get => _lodgingEndDate;
            set
            {
                if (_lodgingEndDate != value)
                {
                    _lodgingEndDate = value;
                    OnPropertyChanged();
                    ApplyHeaderDatesToItems();
                }
            }
        }

        public decimal TotalUsd => Invoice.TotalUsd;
        public decimal TotalPeso => Invoice.TotalPeso;
        public decimal TotalKrw => Invoice.TotalKrw;

        // ===== Commands =====

        public ICommand RefreshRateCommand
        {
            get;
        }
        public ICommand ExportToExcelCommand
        {
            get;
        }
        public ICommand AddItemCommand
        {
            get;
        }
        public ICommand RemoveItemCommand
        {
            get;
        }

        // ===== 로직 =====

        private async Task LoadExchangeRateAsync()
        {
            try
            {
                IsLoadingRate = true;

                // USD -> MXN
                var mxn = await _exchangeRateService.GetRateAsync("MXN");
                if (mxn.HasValue)
                {
                    ExchangeRateUsdToMxn = mxn.Value;
                }

                // USD -> KRW
                var krw = await _exchangeRateService.GetRateAsync("KRW");
                if (krw.HasValue)
                {
                    ExchangeRateUsdToKrw = krw.Value;
                }

                RecalculateTotals();
            }
            finally
            {
                IsLoadingRate = false;
            }
        }

        private void UpdateItemExchangeRates()
        {
            foreach (var item in Items)
            {
                item.ExchangeRate = ExchangeRateUsdToMxn;
                item.ExchangeRateKrw = ExchangeRateUsdToKrw;
            }

            RecalculateTotals();
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InvoiceItem item in e.NewItems)
                {
                    SubscribeItem(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (InvoiceItem item in e.OldItems)
                {
                    item.PropertyChanged -= InvoiceItem_PropertyChanged;
                }
            }

            RecalculateTotals();
        }

        private void SubscribeItem(InvoiceItem item)
        {
            item.PropertyChanged += InvoiceItem_PropertyChanged;
            item.ExchangeRate = ExchangeRateUsdToMxn;
            item.ExchangeRateKrw = ExchangeRateUsdToKrw;
            item.DiscountPercent = GlobalDiscountPercent;
        }

        private void InvoiceItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = sender as InvoiceItem;
            if (item == null) return;

            if (e.PropertyName == nameof(InvoiceItem.ItemType))
            {
                item.UnitPrice = GetDefaultUnitPrice(item.ItemType);

                DateTime start = LodgingStartDate ?? DateTime.Today;
                DateTime end = LodgingEndDate ?? start.AddDays(1);

                if (item.ItemType == "공항픽업" || item.ItemType == "오마카세")
                {
                    item.StartDate = start;
                    item.EndDate = start;
                }
                else
                {
                    item.StartDate = start;
                    item.EndDate = end;
                }

                item.DiscountPercent = GlobalDiscountPercent;
            }

            if (e.PropertyName == nameof(InvoiceItem.StartDate) ||
                e.PropertyName == nameof(InvoiceItem.EndDate) ||
                e.PropertyName == nameof(InvoiceItem.Quantity) ||
                e.PropertyName == nameof(InvoiceItem.UnitPrice) ||
                e.PropertyName == nameof(InvoiceItem.ExchangeRate) ||
                e.PropertyName == nameof(InvoiceItem.ExchangeRateKrw) ||
                e.PropertyName == nameof(InvoiceItem.DiscountPercent))
            {
                RecalculateTotals();
            }
        }

        public void RecalculateTotals()
        {
            OnPropertyChanged(nameof(TotalUsd));
            OnPropertyChanged(nameof(TotalPeso));
            OnPropertyChanged(nameof(TotalKrw));
        }

        private void ApplyHeaderDatesToItems()
        {
            if (!LodgingStartDate.HasValue || !LodgingEndDate.HasValue)
                return;

            DateTime start = LodgingStartDate.Value.Date;
            DateTime end = LodgingEndDate.Value.Date;
            if (end < start) end = start;

            foreach (var item in Items)
            {
                item.StartDate = start;
                item.EndDate = (item.ItemType == "공항픽업" || item.ItemType == "오마카세")
                    ? start
                    : end;
            }

            RecalculateTotals();
        }

        private void AddInitialItem(string itemType)
        {
            DateTime start = LodgingStartDate ?? DateTime.Today;
            DateTime end = LodgingEndDate ?? start.AddDays(1);

            var item = new InvoiceItem
            {
                ItemType = itemType,
                Description = string.Empty,
                Quantity = 1,
                UnitPrice = GetDefaultUnitPrice(itemType),
                ExchangeRate = ExchangeRateUsdToMxn,
                ExchangeRateKrw = ExchangeRateUsdToKrw,
                DiscountPercent = GlobalDiscountPercent
            };

            if (itemType == "공항픽업" || itemType == "오마카세")
            {
                item.StartDate = start;
                item.EndDate = start;
            }
            else
            {
                item.StartDate = start;
                item.EndDate = end;
            }

            Items.Add(item);
        }

        private void AddNewItem()
        {
            DateTime start = LodgingStartDate ?? DateTime.Today;
            DateTime end = LodgingEndDate ?? start.AddDays(1);

            var item = new InvoiceItem
            {
                ItemType = "숙박",
                Description = string.Empty,
                Quantity = 1,
                UnitPrice = GetDefaultUnitPrice("숙박"),
                ExchangeRate = ExchangeRateUsdToMxn,
                ExchangeRateKrw = ExchangeRateUsdToKrw,
                DiscountPercent = GlobalDiscountPercent
            };

            item.StartDate = start;
            item.EndDate = end;

            Items.Add(item);
        }

        private void RemoveItem(InvoiceItem item)
        {
            if (item == null) return;
            Items.Remove(item);
        }

        private decimal GetDefaultUnitPrice(string itemType)
        {
            // 필요하면 항목별 기본 단가 여기서 정의
            switch (itemType)
            {
                case "숙박":
                    return 50m;
                case "출퇴근":
                    return 10m;
                case "공항픽업":
                    return 30m;
                case "오마카세":
                    return 100m;
                case "주말식사":
                    return 20m;
                default:
                    return 0m;
            }
        }

        private void ExportToExcel()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var templatePath = Path.Combine(baseDir, "Templates", "FacturaTemplate.xlsx");

                if (!File.Exists(templatePath))
                {
                    MessageBox.Show("엑셀 템플릿 파일을 찾을 수 없습니다.\n" + templatePath);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    FileName = "Factura_" + Invoice.InvoiceDate.ToString("yyyyMMdd") + ".xlsx"
                };

                if (dialog.ShowDialog() == true)
                {
                    var exporter = new ExcelExportService(templatePath);
                    exporter.Export(Invoice, dialog.FileName);

                    MessageBox.Show("엑셀 파일이 생성되었습니다.\n" + dialog.FileName);

                    // 자동으로 엑셀 열기
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("엑셀 내보내기 중 오류가 발생했습니다.\n" + ex.Message);
            }
        }
    }
}

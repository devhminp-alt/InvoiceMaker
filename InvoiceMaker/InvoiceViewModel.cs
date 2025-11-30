using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using InvoiceMaker.Models;
using InvoiceMaker.Services;
using System.Diagnostics;

namespace InvoiceMaker.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        private readonly ExchangeRateService _exchangeRateService;

        private bool _isLoadingRate;
        private decimal _exchangeRateUsdToMxn; // 1 USD = ? MXN

        private DateTime? _lodgingStartDate;
        private DateTime? _lodgingEndDate;

        public InvoiceViewModel()
        {
            _exchangeRateService = new ExchangeRateService();

            Invoice = new Invoice();
            Invoice.Currency = "USD";

            Items = Invoice.Items;

            // 숙박 시작/종료 디폴트: 오늘 / 내일
            LodgingStartDate = DateTime.Today;
            LodgingEndDate = DateTime.Today.AddDays(1);

            // 항목 타입 리스트
            ItemTypes = new ObservableCollection<string>
            {
                "숙박",
                "오마카세",
                "출퇴근",
                "공항픽업",
                "주말식사"
            };

            // ★ 디폴트 한 줄: 숙박만 추가 ★
            AddInitialItem("숙박");

            // Commands
            RefreshRateCommand = new RelayCommand(async p => await LoadExchangeRateAsync());
            ExportToExcelCommand = new RelayCommand(p => ExportToExcel());
            AddItemCommand = new RelayCommand(p => AddNewItem());
            RemoveItemCommand = new RelayCommand(
                p => RemoveItem(p as InvoiceItem),
                p => p is InvoiceItem);

            foreach (var item in Items)
                SubscribeItem(item);

            Task.Run(async () => await LoadExchangeRateAsync());
        }

        // ===== 프로퍼티 =====

        public Invoice Invoice
        {
            get; private set;
        }

        public ObservableCollection<InvoiceItem> Items
        {
            get; private set;
        }

        public ObservableCollection<string> ItemTypes
        {
            get; private set;
        }

        public bool IsLoadingRate
        {
            get
            {
                return _isLoadingRate;
            }
            set
            {
                if (_isLoadingRate != value)
                {
                    _isLoadingRate = value;
                    OnPropertyChanged();
                }
            }
        }

        // 1 USD = ? MXN
        public decimal ExchangeRateUsdToMxn
        {
            get
            {
                return _exchangeRateUsdToMxn;
            }
            set
            {
                if (_exchangeRateUsdToMxn != value)
                {
                    _exchangeRateUsdToMxn = value;
                    Invoice.ExchangeRate = value;
                    OnPropertyChanged();

                    // 모든 항목에 환율 반영
                    foreach (var item in Items)
                    {
                        item.ExchangeRate = value;
                    }

                    RecalculateTotals();
                }
            }
        }

        public decimal TotalUsd => Items.Sum(i => i.AmountUsd);
        public decimal TotalPeso => Items.Sum(i => i.AmountPeso);

        // 숙박 시작/종료 (헤더)
        public DateTime? LodgingStartDate
        {
            get
            {
                return _lodgingStartDate;
            }
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
            get
            {
                return _lodgingEndDate;
            }
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

        // ===== Commands =====

        public ICommand RefreshRateCommand
        {
            get; private set;
        }
        public ICommand ExportToExcelCommand
        {
            get; private set;
        }
        public ICommand AddItemCommand
        {
            get; private set;
        }
        public ICommand RemoveItemCommand
        {
            get; private set;
        }

        // ===== 메서드 =====

        private async Task LoadExchangeRateAsync()
        {
            try
            {
                IsLoadingRate = true;

                var rate = await _exchangeRateService.GetUsdToMxnAsync();
                if (rate.HasValue)
                {
                    ExchangeRateUsdToMxn = rate.Value;
                }

                RecalculateTotals();
            }
            finally
            {
                IsLoadingRate = false;
            }
        }

        public void RecalculateTotals()
        {
            OnPropertyChanged(nameof(TotalUsd));
            OnPropertyChanged(nameof(TotalPeso));
        }

        private decimal GetDefaultUnitPrice(string itemType)
        {
            // 항목별 USD 단가 – 값은 필요에 맞게 조정
            switch (itemType)
            {
                case "숙박": return 50m;
                case "오마카세": return 120m;
                case "출퇴근": return 10m;
                case "공항픽업": return 30m;
                case "주말식사": return 20m;
                default: return 0m;
            }
        }
        private void AddInitialItem(string itemType)
        {
            DateTime start = LodgingStartDate ?? DateTime.Today;
            DateTime end = LodgingEndDate ?? start.AddDays(1);

            var item = new InvoiceItem
            {
                ItemType = itemType,
                Description = "",
                Quantity = 1,
                UnitPrice = GetDefaultUnitPrice(itemType),
                ExchangeRate = ExchangeRateUsdToMxn
            };

            // ★ 공항픽업 + 오마카세는 1일만 적용
            if (itemType == "공항픽업" || itemType == "오마카세")
            {
                item.StartDate = start;
                item.EndDate = start;   // 하루
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
                Description = "",
                Quantity = 1,
                UnitPrice = GetDefaultUnitPrice("숙박"),
                ExchangeRate = ExchangeRateUsdToMxn,
                StartDate = start,
                EndDate = end
            };

            Items.Add(item);
            SubscribeItem(item);
            RecalculateTotals();
        }

        private void RemoveItem(InvoiceItem item)
        {
            if (item == null) return;

            item.PropertyChanged -= InvoiceItem_PropertyChanged;
            Items.Remove(item);
            RecalculateTotals();
        }

        private void SubscribeItem(InvoiceItem item)
        {
            item.PropertyChanged += InvoiceItem_PropertyChanged;
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

                // ★ 공항픽업 + 오마카세는 하루 처리
                if (item.ItemType == "공항픽업" || item.ItemType == "오마카세")
                {
                    item.StartDate = start;
                    item.EndDate = start;   // dias = 1
                }
                else
                {
                    item.StartDate = start;
                    item.EndDate = end;
                }

                RecalculateTotals();
            }
        }

        // 헤더 날짜 바뀌면: 공항픽업 제외한 항목들에 기본값 반영
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
                item.EndDate = (item.ItemType == "공항픽업" || item.ItemType == "오마카세") ? start : end;
            }

            RecalculateTotals();
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

                    // 엑셀 저장 성공 메시지
                    MessageBox.Show("엑셀 파일이 생성되었습니다.\n" + dialog.FileName);

                    // 🔥 자동으로 엑셀 파일 열기
                    System.Diagnostics.Process.Start(new ProcessStartInfo(dialog.FileName)
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

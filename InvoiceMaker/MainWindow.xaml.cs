using System.Windows;
using System.Windows.Controls;
using InvoiceMaker.ViewModels;

namespace InvoiceMaker
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = DataContext as InvoiceViewModel;
            if (vm != null)
            {
                var comboCol = ItemsGrid.Columns[0] as DataGridComboBoxColumn;
                if (comboCol != null)
                {
                    comboCol.ItemsSource = vm.ItemTypes;
                }
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var vm = DataContext as InvoiceViewModel;
            if (vm != null)
            {
                vm.RecalculateTotals();
            }
        }
    }
}

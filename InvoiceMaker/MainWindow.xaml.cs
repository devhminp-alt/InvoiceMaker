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
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (DataContext is InvoiceViewModel vm)
            {
                vm.RecalculateTotals();
            }
        }
    }
}

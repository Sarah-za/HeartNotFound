using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Remotemonitor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(SearchBox);
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text?.Length ?? 0;
        }

        private void PatientList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            // Entfernte abwählen
            foreach (var removed in e.RemovedItems.OfType<VitalSample>())
            {
                vm.Selected.Remove(removed);
                removed.IsActive = false;
            }

            // Neue Auswahl: nur bis max 8 erlauben
            foreach (var added in e.AddedItems.OfType<VitalSample>())
            {
                // Wenn schon ausgewählt, nichts tun
                if (vm.Selected.Contains(added))
                {
                    added.IsActive = true;
                    continue;
                }

                if (vm.Selected.Count >= 8)
                {
                    // Limit erreicht -> UI-Auswahl rückgängig + Häkchen AUS
                    added.IsActive = false;

                    PatientList.SelectionChanged -= PatientList_SelectionChanged;
                    PatientList.SelectedItems.Remove(added);
                    PatientList.SelectionChanged += PatientList_SelectionChanged;

                    continue;
                }

                // akzeptiert
                vm.Selected.Add(added);
                added.IsActive = true;
            }
        }




        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (e.Key == Key.Enter)
            {
                var first = vm.VitalsView.Cast<object>().OfType<VitalSample>().FirstOrDefault();
                if (first != null)
                {
                    PatientList.SelectedItems.Clear();
                    PatientList.SelectedItem = first;
                    if (!vm.Selected.Contains(first)) vm.Selected.Add(first);
                    PatientList.ScrollIntoView(first);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                vm.SearchText = string.Empty;
                Keyboard.Focus(SearchBox);
                SearchBox.SelectAll();
                e.Handled = true;
            }
        }

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            if ((sender as FrameworkElement)?.DataContext is VitalSample v)
            {
                OpenHistory(v);
                e.Handled = true;
            }
        }

        private void PatientList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PatientList.SelectedItem is VitalSample patient)
            {
                OpenHistory(patient);
            }
        }

        private void OpenHistory(VitalSample patient)
        {
            var win = new VitalHistoryWindow(patient)
            {
                Owner = this
            };
            win.Show();
        }
    }

}
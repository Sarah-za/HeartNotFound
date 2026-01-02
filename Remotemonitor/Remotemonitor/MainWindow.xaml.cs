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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Beim Laden: Fokus sicher ins Suchfeld setzen
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(SearchBox);
            SearchBox.Focus();
            SearchBox.CaretIndex = SearchBox.Text?.Length ?? 0;
        }

        // Sync ListView-Auswahl -> Selected im ViewModel
        private void PatientList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            foreach (var added in e.AddedItems)
                if (added is VitalSample s && !vm.Selected.Contains(s))
                    vm.Selected.Add(s);

            foreach (var removed in e.RemovedItems)
                if (removed is VitalSample s)
                    vm.Selected.Remove(s);

            foreach (VitalSample p in e.AddedItems)
                p.IsActive = true;

            foreach (VitalSample p in e.RemovedItems)
                p.IsActive = false;
        }

        // ENTER in Suchbox: ersten Treffer auswählen | ESC: löschen
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

        // (früherer) Button: Suchen – existiert ggf. nicht mehr, kann bleiben
        private void SearchSelectFirst_Click(object sender, RoutedEventArgs e)
        {
            SearchBox_KeyDown(SearchBox,
                new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Enter)
                { RoutedEvent = Keyboard.KeyDownEvent });
        }

        // (früherer) Button: Leeren – existiert ggf. nicht mehr, kann bleiben
        private void SearchClear_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            vm.SearchText = string.Empty;
            Keyboard.Focus(SearchBox);
            SearchBox.SelectAll();
        }

        /*

        // NEU: Doppelklick auf Karte -> Detailfenster
        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Nur bei Doppelklick reagieren
            if (e.ClickCount != 2) return;

            if ((sender as FrameworkElement)?.DataContext is VitalSample v)
            {
                var win = new PatientDetailWindow
                {
                    Owner = this,
                    DataContext = v
                };
                e.Handled = true; // verhindert, dass ScrollViewer/ListView weiterreagiert
                win.ShowDialog();
            }
        }
         */

        /***
        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Nur bei Doppelklick reagieren
            if (e.ClickCount != 2) return;

            if ((sender as FrameworkElement)?.DataContext is VitalSample v)
            {
                var win = new Remotmonitor.Views.VitalHistoryWindow(v)
                {
                    Owner = this,
                };
                e.Handled = true; // verhindert, dass ScrollViewer/ListView weiterreagiert
                win.ShowDialog();
            }

        }


    private void PatientList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PatientList.SelectedItem is not VitalSample patient)
            return;

        var win = new ThresholdWindow(patient)
        {
            Owner = this
        };

        bool? result = win.ShowDialog();

        if (result == true)
        {
            // Nach Änderung Alarmfarbe neu berechnen
            patient.EvaluateAlarmBrush();
        }
    }
        ***/

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
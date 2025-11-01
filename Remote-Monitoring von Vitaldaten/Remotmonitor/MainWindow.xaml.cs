using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Remotmonitor.Models;
using Remotmonitor.ViewModels;

namespace Remotmonitor;

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

    // Button: Suchen (wie Enter)
    private void SearchSelectFirst_Click(object sender, RoutedEventArgs e)
    {
        SearchBox_KeyDown(SearchBox,
            new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, Key.Enter)
            { RoutedEvent = Keyboard.KeyDownEvent });
    }

    // Button: Leeren
    private void SearchClear_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        vm.SearchText = string.Empty;
        Keyboard.Focus(SearchBox);
        SearchBox.SelectAll();
    }
}


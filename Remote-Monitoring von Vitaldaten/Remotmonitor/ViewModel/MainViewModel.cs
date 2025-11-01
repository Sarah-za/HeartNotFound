using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Remotmonitor.Models;
using Remotmonitor.Services;

namespace Remotmonitor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // Alle Patienten (Live-Feed)
    [ObservableProperty]
    private ObservableCollection<VitalSample> vitals = new();

    // Gefilterter/Sortierter Blick auf Vitals (für die ListView)
    public ICollectionView VitalsView { get; }

    // Ausgewählte Patienten (unten als Kacheln)
    public ObservableCollection<VitalSample> Selected { get; } = new();

    // Spaltenanzahl unten (1..4)
    public int Columns => Selected.Count == 0 ? 1 : System.Math.Min(Selected.Count, 4);

    // Suchtext (bindet an TextBox)
    [ObservableProperty]
    private string? searchText;

    private readonly IDataSource _source;

    public MainViewModel(IDataSource source)
    {
        _source = source;

        // CollectionView auf die ObservableCollection aufsetzen
        VitalsView = CollectionViewSource.GetDefaultView(Vitals);
        VitalsView.Filter = FilterPatient;
        VitalsView.SortDescriptions.Clear();
        VitalsView.SortDescriptions.Add(new SortDescription(nameof(VitalSample.PatientId), ListSortDirection.Ascending));

        Selected.CollectionChanged += (_, __) => OnPropertyChanged(nameof(Columns));

        _source.OnSample += s =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                // vorhandenen Patienten in-place aktualisieren
                var index = -1;
                for (int i = 0; i < Vitals.Count; i++)
                    if (Vitals[i].PatientId == s.PatientId) { index = i; break; }

                if (index >= 0)
                {
                    var v = Vitals[index];
                    v.Ts = s.Ts;
                    v.Hr = s.Hr;
                    v.Spo2 = s.Spo2;
                    v.Rr = s.Rr;
                    v.Temp = s.Temp;
                    v.MonitorId = s.MonitorId;
                }
                else
                {
                    Vitals.Add(s);
                }

                // Bei eingestellter Suche nachführen
                VitalsView.Refresh();
            });
        };
    }

    // Live-Filter: PatientId, MonitorId, oder beliebiger Textteil
    private bool FilterPatient(object obj)
    {
        if (obj is not VitalSample v) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var q = SearchText.Trim();
        // Case-insensitive Contains
        return (v.PatientId?.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
               (v.MonitorId?.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // Wenn sich der Suchtext ändert -> View refreshen
    partial void OnSearchTextChanged(string? value)
    {
        VitalsView.Refresh();
    }

    public async System.Threading.Tasks.Task StartAsync()
    {
        await _source.StartAsync(System.Threading.CancellationToken.None);
    }
}

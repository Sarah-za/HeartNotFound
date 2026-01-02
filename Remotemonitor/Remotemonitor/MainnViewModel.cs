using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Remotemonitor
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<VitalSample> vitals = new();

        // Gefilterte/Sortierte/Gruppierte Sicht für die Liste
        public ICollectionView VitalsView { get; }

        // Rooms (für Filter-Combo) – enthält "Alle Zimmer" + Zimmernummern (z. B. "101")
        public ObservableCollection<string> Rooms { get; } = new();

        // "Alle Zimmer" = kein Filter
        [ObservableProperty]
        private string selectedRoom = "Alle Zimmer";

        // Auswahl (für Karten)
        public ObservableCollection<VitalSample> Selected { get; } = new();

        // Nur die ersten 8 Karten anzeigen
        public IEnumerable<VitalSample> SelectedTop8 => Selected.Take(8);

        // Spaltenanzahl unten (1..4)
        public int Columns
        {
            get
            {
                int n = Selected.Count;
                if (n > 8) n = 8;
                return n == 0 ? 1 : Math.Min(n, 4);
            }
        }

        // Suche
        [ObservableProperty]
        private string? searchText;

        private readonly IDataSource _source;

        public MainViewModel(IDataSource source)
        {
            _source = source;

            VitalsView = CollectionViewSource.GetDefaultView(Vitals);
            VitalsView.Filter = FilterPatient;

            // Sortierung: Zimmer -> Bett -> PatientId
            VitalsView.SortDescriptions.Clear();
            VitalsView.SortDescriptions.Add(new SortDescription(nameof(VitalSample.Room), ListSortDirection.Ascending));
            VitalsView.SortDescriptions.Add(new SortDescription(nameof(VitalSample.Bed), ListSortDirection.Ascending));
            VitalsView.SortDescriptions.Add(new SortDescription(nameof(VitalSample.PatientId), ListSortDirection.Ascending));

            // Gruppierung nur nach Zimmer
            VitalsView.GroupDescriptions.Clear();
            VitalsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(VitalSample.Room)));

            // Live-Shaping aktivieren (kein Refresh im Tick nötig)
            if (VitalsView is ICollectionViewLiveShaping live)
            {
                live.IsLiveSorting = true;
                live.IsLiveGrouping = true;
                live.IsLiveFiltering = true;

                live.LiveSortingProperties.Add(nameof(VitalSample.Room));
                live.LiveSortingProperties.Add(nameof(VitalSample.Bed));
                live.LiveSortingProperties.Add(nameof(VitalSample.PatientId));

                live.LiveGroupingProperties.Add(nameof(VitalSample.Room));

                live.LiveFilteringProperties.Add(nameof(VitalSample.Room));
                live.LiveFilteringProperties.Add(nameof(VitalSample.PatientId));
                live.LiveFilteringProperties.Add(nameof(VitalSample.MonitorId));
            }

            Rooms.Add("Alle Zimmer"); // erster Eintrag

            // Änderungen der Auswahl -> UI aktualisieren
            Selected.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(Columns));
                OnPropertyChanged(nameof(SelectedTop8));
            };

            _source.OnSample += s =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    // In-place Update, damit Auswahl stabil bleibt
                    var index = -1;
                    for (int i = 0; i < Vitals.Count; i++)
                        if (Vitals[i].PatientId == s.PatientId) { index = i; break; }

                    if (index >= 0)
                    {
                        var v = Vitals[index];

                        // Demographie
                        v.Gender = s.Gender;
                        v.Age = s.Age;

                        // Zimmer + Bett
                        v.Room = s.Room;   // z. B. "101"
                        v.Bed = s.Bed;    // 1..N

                        // Messwerte
                        v.Ts = s.Ts;
                        v.Hr = s.Hr;
                        v.Spo2 = s.Spo2;
                        v.Rr = s.Rr;
                        v.Temp = s.Temp;
                        v.Sys = s.Sys;
                        v.Dia = s.Dia;
                        v.MonitorId = s.MonitorId;
                    }
                    else
                    {
                        Vitals.Add(s);
                    }

                    // Rooms-Liste aktuell halten (nur Zimmernummern)
                    if (!Rooms.Contains(s.Room))
                        InsertRoomSorted(s.Room);

                    // KEIN VitalsView.Refresh() hier – Live-Shaping übernimmt das
                });
            };
        }

        private void InsertRoomSorted(string room)
        {
            // Rooms[0] ist "Alle Zimmer" – ab Index 1 sortiert einfügen
            int insertIdx = 1;
            while (insertIdx < Rooms.Count && string.Compare(Rooms[insertIdx], room, System.StringComparison.Ordinal) < 0)
                insertIdx++;
            Rooms.Insert(insertIdx, room);
        }

        private bool FilterPatient(object obj)
        {
            if (obj is not VitalSample v) return false;

            // Zimmer-Filter (nur Zimmernummer, nicht Bett)
            if (!string.IsNullOrWhiteSpace(SelectedRoom) && SelectedRoom != "Alle Zimmer")
            {
                if (!string.Equals(v.Room, SelectedRoom, System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Textsuche (ID / Monitor)
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            var q = SearchText.Trim();
            return (v.PatientId?.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (v.MonitorId?.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        //  Normales Refresh bei User-Aktionen (OK, passiert selten)
        partial void OnSearchTextChanged(string? value) => VitalsView.Refresh();
        partial void OnSelectedRoomChanged(string value) => VitalsView.Refresh();

        public async Task StartAsync()
        {
            await _source.StartAsync(CancellationToken.None);
        }
    }
}

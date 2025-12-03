using System.Reflection;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Remotmonitor.Models;

// Benachrichtigungsfähiges Model: UI aktualisiert sich bei Feldänderungen
public partial class VitalSample : ObservableObject
{
    [ObservableProperty] private string patientId = "P-0001";
    [ObservableProperty] private string monitorId = "MON-01";

    // Demographie
    [ObservableProperty] private string gender = "m"; // "m", "w", "d"
    [ObservableProperty] private int age = 50;        // Jahre

    // Zimmer (z. B. 101) + Bett (1..N)
    [ObservableProperty] private string room = "101";
    [ObservableProperty] private int bed = 1;

    // Abgeleitet: "101-3"
    public string RoomBed => $"{Room}-{Bed}";

    // Zeit intern in UTC; Anzeige in TsLocal
    [ObservableProperty] private System.DateTime ts;
    public System.DateTime TsLocal => Ts.ToLocalTime();

    // Vitalwerte
    [ObservableProperty] private int hr;      // Herzfrequenz (bpm)
    [ObservableProperty] private int spo2;    // Sauerstoffsättigung (%)
    [ObservableProperty] private int rr;      // Atemfrequenz (/min)
    [ObservableProperty] private double temp; // Temperatur (°C)
    [ObservableProperty] private int sys;     // Systolisch (mmHg)
    [ObservableProperty] private int dia;     // Diastolisch (mmHg)

    // Für die Tabellenanzeige "SYS/DIA"
    public string Bp => $"{Sys}/{Dia}";

    // Anzeigename: "P-0001 (m, 62)"
    public string DisplayName => $"{PatientId} ({Gender}, {Age})";

    // Abgeleitete Alarmfarbe — jetzt mit individuellen Limits
    public SolidColorBrush AlarmColor => EvaluateAlarmBrush();

    // Änderungen melden
    partial void OnTsChanged(System.DateTime value) => OnPropertyChanged(nameof(TsLocal));

    partial void OnHrChanged(int value) => OnPropertyChanged(nameof(AlarmColor));
    partial void OnSpo2Changed(int value) => OnPropertyChanged(nameof(AlarmColor));
    partial void OnRrChanged(int value) => OnPropertyChanged(nameof(AlarmColor));
    partial void OnTempChanged(double value) => OnPropertyChanged(nameof(AlarmColor));

    partial void OnSysChanged(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(Bp));
    }
    partial void OnDiaChanged(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(Bp));
    }

    // Abgeleitete Anzeigen aktualisieren
    partial void OnPatientIdChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnGenderChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnAgeChanged(int value) => OnPropertyChanged(nameof(DisplayName));

    partial void OnRoomChanged(string value) => OnPropertyChanged(nameof(RoomBed));
    partial void OnBedChanged(int value) => OnPropertyChanged(nameof(RoomBed));
}

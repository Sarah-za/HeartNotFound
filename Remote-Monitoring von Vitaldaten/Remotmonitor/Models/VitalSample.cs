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

    // Abgeleitete Alarmfarbe
    public SolidColorBrush AlarmColor => GetAlarmBrush();

    private SolidColorBrush GetAlarmBrush()
    {
        bool critical =
            Spo2 < 90 ||
            Hr < 40 || Hr > 130 ||
            Rr < 8 || Rr > 25 ||
            Temp < 35.5 || Temp > 38.5 ||
            Sys > 180 || Sys < 80 || Dia > 110 || Dia < 50;

        if (critical) return new SolidColorBrush(Colors.Red);

        bool warning =
            (Spo2 is >= 90 and <= 93) ||
            (Hr is >= 40 and <= 49) || (Hr is >= 111 and <= 130) ||
            (Rr is >= 8 and <= 9) || (Rr is >= 21 and <= 25) ||
            (Temp is >= 35.5 and <= 35.9) || (Temp is >= 37.6 and <= 38.5) ||
            (Sys is >= 140 and <= 180) || (Sys is >= 80 and <= 89) ||
            (Dia is >= 90 and <= 110) || (Dia is >= 50 and <= 59);

        if (warning) return new SolidColorBrush(Colors.Gold);
        return new SolidColorBrush(Colors.Lime);
    }

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

using System.Reflection;
using System.Windows.Controls;
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

    public string Status => IsStale ? "Keine Daten" : "OK";

    public bool IsStale
    {
        get
        {
            var age = (DateTime.UtcNow - Ts).TotalSeconds;
            return age > 30;
        }
    }

    [ObservableProperty] private int stalePulse;

    partial void OnStalePulseChanged(int value)
    {
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(Status));
    }


    // Für die Tabellenanzeige "SYS/DIA"
    public string Bp => $"{Sys}/{Dia}";

    // Anzeigename: "P-0001 (m, 62)"
    public string DisplayName => $"{PatientId} ({Gender}, {Age})";

    // Abgeleitete Alarmfarbe — jetzt mit individuellen Limits
    public SolidColorBrush AlarmColor => EvaluateAlarmBrush();

    // Änderungen melden
    partial void OnTsChanged(System.DateTime value)
    {
        OnPropertyChanged(nameof(TsLocal));
        OnPropertyChanged(nameof(IsStale));
        OnPropertyChanged(nameof(Status));
    }

    partial void OnHrChanged(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(EWS));
    }
    partial void OnSpo2Changed(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(EWS));
    }
    partial void OnRrChanged(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(EWS));
    }
    partial void OnTempChanged(double value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(EWS));
    }

    partial void OnSysChanged(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(Bp));
        OnPropertyChanged(nameof(EWS));
    }
    partial void OnDiaChanged(int value)
    {
        OnPropertyChanged(nameof(AlarmColor));
        OnPropertyChanged(nameof(Bp));
        OnPropertyChanged(nameof(EWS));
    }

    public int EWS
    {
        get
        {
            int score = 0;

            //HR
            if (Hr <= 40) score += 2;
            else if (Hr > 40 && Hr <= 50) score += 1;
            else if (Hr >= 91 && Hr <= 110) score += 1;
            else if (Hr > 110 && Hr <= 130) score += 2;
            else if (Hr > 130) score += 3;

            //Temp
            if (Temp < 35.0) score += 3;
            else if (Temp < 36.0 && Temp > 35.0) score += 1;
            else if (Temp > 38.0 && Temp <= 39.0) score += 1;
            else if (Temp > 39.0) score += 2;

            //BP
            if (Sys <= 90) score += 3;
            else if (Sys > 90 && Sys <= 100) score += 2;
            else if (Sys > 100 && Sys <= 110) score += 1;
            else if (Sys >= 220) score += 3;

            //RR
            if (Rr < 8) score += 3;
            else if (Rr >= 9 && Rr <= 11) score += 1;
            else if (Rr > 21 && Rr <= 24) score += 2;
            else if (Rr > 24) score += 3;

            //Spo2
            if (Spo2 <= 91) score += 3;
            else if (Spo2 >= 92 && Spo2 <= 93) score += 2;
            else if (Spo2 >= 94 && Spo2 <= 95) score += 1;

            return score;
        }
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }


    // Abgeleitete Anzeigen aktualisieren
    partial void OnPatientIdChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnGenderChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnAgeChanged(int value) => OnPropertyChanged(nameof(DisplayName));

    partial void OnRoomChanged(string value) => OnPropertyChanged(nameof(RoomBed));
    partial void OnBedChanged(int value) => OnPropertyChanged(nameof(RoomBed));
}

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

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}";

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

    public string CardHeaderLine => $"{PatientId} | Zimmer/Bett: {RoomBed} | Monitor: {MonitorId}";

    private const int MaxHistorySeconds = 3600; // 1 Stunde

    public List<VitalSnapshot> History { get; } = new();

    private int _ewsHr, _ewsSpo2, _ewsRr, _ewsTemp, _ewsSys;
    private int _ewsTotal;

    public int EwsHr => _ewsHr;
    public int EwsSpo2 => _ewsSpo2;
    public int EwsRr => _ewsRr;
    public int EwsTemp => _ewsTemp;
    public int EwsSys => _ewsSys;

    public int EWS => _ewsTotal;

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
        RecalculateEws();
        OnPropertyChanged(nameof(EwsHr));
        OnPropertyChanged(nameof(EWS));
        OnPropertyChanged(nameof(AlarmColor));
    }

    partial void OnSpo2Changed(int value)
    {
        RecalculateEws();
        OnPropertyChanged(nameof(EwsSpo2));
        OnPropertyChanged(nameof(EWS));
        OnPropertyChanged(nameof(AlarmColor));
    }

    partial void OnRrChanged(int value)
    {
        RecalculateEws();
        OnPropertyChanged(nameof(EwsRr));
        OnPropertyChanged(nameof(EWS));
        OnPropertyChanged(nameof(AlarmColor));
    }

    partial void OnTempChanged(double value)
    {
        RecalculateEws();
        OnPropertyChanged(nameof(EwsTemp));
        OnPropertyChanged(nameof(EWS));
        OnPropertyChanged(nameof(AlarmColor));
    }

    partial void OnSysChanged(int value)
    {
        RecalculateEws();
        OnPropertyChanged(nameof(EwsSys));
        OnPropertyChanged(nameof(EWS));
        OnPropertyChanged(nameof(Bp));
        OnPropertyChanged(nameof(AlarmColor));
    }

    public void RecalculateEws()
    {
        // HR
        _ewsHr =
            (Hr <= 40) ? 2 :
            (Hr <= 50) ? 1 :
            (Hr >= 91 && Hr <= 110) ? 1 :
            (Hr <= 130) ? 2 :
            (Hr > 130) ? 3 :
            0;

        // SpO2
        _ewsSpo2 =
            (Spo2 <= 91) ? 3 :
            (Spo2 <= 93) ? 2 :
            (Spo2 <= 95) ? 1 :
            0;

        // RR
        _ewsRr =
            (Rr < 8) ? 3 :
            (Rr <= 11) ? 1 :
            (Rr <= 20) ? 0 :
            (Rr <= 24) ? 2 :
            3;

        // Temp
        _ewsTemp =
            (Temp < 35.0) ? 3 :
            (Temp < 36.0) ? 1 :
            (Temp <= 38.0) ? 0 :
            (Temp <= 39.0) ? 1 :
            2;

        // Sys
        _ewsSys =
            (Sys <= 90) ? 3 :
            (Sys <= 100) ? 2 :
            (Sys <= 110) ? 1 :
            (Sys >= 220) ? 3 :
            0;

        _ewsTotal = _ewsHr + _ewsSpo2 + _ewsRr + _ewsTemp + _ewsSys;
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

    // Nötig um jede Sekunde Daten zu speichern für Graphen
    public void AddSnapshot()
    {
        History.Add(new VitalSnapshot
        {
            Ts = DateTime.UtcNow,
            Hr = Hr,
            Spo2 = Spo2,
            Rr = Rr,
            Temp = Temp,
            Sys = Sys
        });

        if (History.Count > MaxHistorySeconds)
            History.RemoveAt(0);
    }


    // Abgeleitete Anzeigen aktualisieren
    partial void OnPatientIdChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnGenderChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnAgeChanged(int value) => OnPropertyChanged(nameof(DisplayName));

    partial void OnRoomChanged(string value) => OnPropertyChanged(nameof(RoomBed));
    partial void OnBedChanged(int value) => OnPropertyChanged(nameof(RoomBed));


}

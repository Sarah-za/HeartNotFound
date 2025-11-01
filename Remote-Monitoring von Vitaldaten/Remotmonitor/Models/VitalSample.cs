using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace Remotmonitor.Models;

// Benachrichtigungsfähiges Model: UI aktualisiert sich bei Feldänderungen
public partial class VitalSample : ObservableObject
{
    [ObservableProperty] private string patientId = "P-0001";
    [ObservableProperty] private string monitorId = "MON-01";

    // Zeit wird intern in UTC gehalten (gut für Netz/Sync)
    [ObservableProperty] private System.DateTime ts;

    [ObservableProperty] private int hr;
    [ObservableProperty] private int spo2;
    [ObservableProperty] private int rr;
    [ObservableProperty] private double temp;

    // Lokale Anzeigezeit abgeleitet aus UTC
    public System.DateTime TsLocal => Ts.ToLocalTime();

    // 🔔 Abgeleitete Alarmfarbe (Lime = normal, Gold = Warnung, Red = kritisch)
    public SolidColorBrush AlarmColor => GetAlarmBrush();

    private SolidColorBrush GetAlarmBrush()
    {
        bool critical =
            Spo2 < 90 ||
            Hr < 40 || Hr > 130 ||
            Rr < 8 || Rr > 25 ||
            Temp < 35.5 || Temp > 38.5;

        bool warning =
            (Spo2 >= 90 && Spo2 <= 93) ||
            (Hr is >= 40 and <= 49) || (Hr is >= 111 and <= 130) ||
            (Rr is >= 8 and <= 9) || (Rr is >= 21 and <= 25) ||
            (Temp is >= 35.5 and <= 35.9) || (Temp is >= 37.6 and <= 38.5);

        if (critical) return new SolidColorBrush(Colors.Red);
        if (warning) return new SolidColorBrush(Colors.Gold);
        return new SolidColorBrush(Colors.Lime);
    }

    // Änderungen melden
    partial void OnHrChanged(int value) => OnPropertyChanged(nameof(AlarmColor));
    partial void OnSpo2Changed(int value) => OnPropertyChanged(nameof(AlarmColor));
    partial void OnRrChanged(int value) => OnPropertyChanged(nameof(AlarmColor));
    partial void OnTempChanged(double value) => OnPropertyChanged(nameof(AlarmColor));

    // Wenn UTC-Zeit geändert wird, die lokale Anzeigezeit aktualisieren
    partial void OnTsChanged(System.DateTime value) => OnPropertyChanged(nameof(TsLocal));
}

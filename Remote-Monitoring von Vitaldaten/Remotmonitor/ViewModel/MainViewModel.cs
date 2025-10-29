using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Remotmonitor.Models;
using Remotmonitor.Services;

namespace Remotmonitor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<VitalSample> vitals = new();

    private readonly IDataSource _source;

    public MainViewModel(IDataSource source)
    {
        _source = source;

        _source.OnSample += s =>
        {
            // Auf UI-Thread aktualisieren
            App.Current.Dispatcher.Invoke(() =>
            {
                // Patientenzeile ersetzen oder anlegen
                var index = -1;
                for (int i = 0; i < Vitals.Count; i++)
                {
                    if (Vitals[i].PatientId == s.PatientId)
                    {
                        index = i;
                        break;
                    }
                }

                if (index >= 0) Vitals[index] = s;
                else Vitals.Add(s);
            });
        };
    }

    // Nur EIN StartAsync!
    public async Task StartAsync()
    {
        await _source.StartAsync(CancellationToken.None);
    }
}


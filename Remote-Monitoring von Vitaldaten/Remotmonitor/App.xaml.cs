using System.Windows;
using Remotmonitor.ViewModels;
using Remotmonitor.Services;

namespace Remotmonitor;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IDataSource source = new MockGeneratorSource();

        var vm = new MainViewModel(source);
        var window = new MainWindow { DataContext = vm };
        window.Show();

        await vm.StartAsync();
    }
}
using System.Configuration;
using System.Data;
using System.Windows;

namespace Remotemonitor
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //IDataSource source = new MockGeneratorSource(); //Mock Daten
            IDataSource source = new MqttDataSource();

            var vm = new MainViewModel(source);
            var window = new MainWindow { DataContext = vm };
            window.Show();

            await vm.StartAsync();
        }
    }

}

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

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var dlg = new ConfigDialog();
            bool? ok = dlg.ShowDialog();

            if (ok != true)
            {
                Shutdown();
                return;
            }

            Current.Properties["MQTT_BROKER"] = dlg.MqttBroker;
            Current.Properties["MQTT_PORT"] = dlg.MqttPort;
            Current.Properties["MQTT_USER"] = dlg.MqttUsername;
            Current.Properties["MQTT_PASS"] = dlg.MqttPassword;

            Current.Properties["DB_SERVER"] = dlg.DbServer;
            Current.Properties["DB_NAME"] = dlg.DbName;
            Current.Properties["DB_USER"] = dlg.DbUser;
            Current.Properties["DB_PASS"] = dlg.DbPassword;

            // Jetzt erst MainWindow starten
            IDataSource source = new MqttDataSource();

            var vm = new MainViewModel(source);
            var window = new MainWindow { DataContext = vm };
            MainWindow = window;

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            window.Show();
            await vm.StartAsync();
        }
    }
}
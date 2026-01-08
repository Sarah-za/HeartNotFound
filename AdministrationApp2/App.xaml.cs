using System.Configuration;
using System.Data;
using System.Windows;

namespace AdministrationApp2
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var dlg = new DbConfigWindow();
            bool? ok = dlg.ShowDialog();

            if (ok != true)
            {
                Shutdown();
                return;
            }

            // Werte global speichern
            Current.Properties["DB_SERVER"] = dlg.Server;
            Current.Properties["DB_NAME"] = dlg.Database;
            Current.Properties["DB_USER"] = dlg.User;
            Current.Properties["DB_PASS"] = dlg.Password;

            var main = new MainWindow();
            MainWindow = main;

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
    }
}

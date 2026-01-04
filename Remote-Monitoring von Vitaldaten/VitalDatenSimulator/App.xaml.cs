using System.Configuration;
using System.Data;
using System.Windows;

namespace VitalDatenSimulator
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var dlg = new InputDialog();
            bool? ok = dlg.ShowDialog();

            if (ok != true)
            {
                Shutdown();
                return;
            }

            // StationID an MainWindow weiterreichen
            Current.Properties["StationID"] = dlg.EnteredID;

            var main = new MainWindow();
            MainWindow = main;

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
    }
}
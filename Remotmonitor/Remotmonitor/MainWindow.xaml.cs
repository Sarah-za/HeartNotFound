using System.Windows;

namespace Remotmonitor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent(); // wird jetzt wieder gefunden, weil x:Class stimmt
    }
}

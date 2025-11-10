using System.Windows;

namespace Remotmonitor
{
    public partial class PatientDetailWindow : Window
    {
        public PatientDetailWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
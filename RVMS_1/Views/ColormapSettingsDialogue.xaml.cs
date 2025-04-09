using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RawVision.Views
{
    /// <summary>
    /// Interaction logic for ColormapSettingsDialogue.xaml
    /// </summary>
    public partial class ColormapSettingsDialogue : Window
    {
        public string SelectColormapName;
        public ColormapSettingsDialogue(string previousValue)
        {
            InitializeComponent();
            DisableCurrentSettingButton(previousValue);
        }

        private void DisableCurrentSettingButton(string setting)
        {
            switch (setting)
            {
                case "Thermal":
                    Thermal.IsEnabled = false;
                    break;
                default:
                    break;
            }
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            this.SelectColormapName = (sender as Button).Name;
            this.DialogResult = true; // Close the dialog with a "true" result
            this.Close(); // Ensure the dialog closes
        }
    }
}

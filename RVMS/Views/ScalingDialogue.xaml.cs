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
using RVMS.ViewModels;

namespace RVMS.Views
{
    /// <summary>
    /// Interaction logic for ScalingDialogue.xaml
    /// </summary>
    public partial class ScalingDialogue : Window
    {
        public string SelectedScalingMethod;
        public ScalingDialogue(string previousValue)
        {
            InitializeComponent();
            DisableCurrentSettingButton(previousValue);
        }

        private void DisableCurrentSettingButton(string setting)
        {
            switch (setting)
            {
                case "Linear":
                    Linear.IsEnabled = false;
                    break;
                case "Log10":
                    Log10.IsEnabled = false;
                    break;
            }
        }

        private void ButtonClicked(object sender, RoutedEventArgs e)
        {
            this.SelectedScalingMethod = (sender as Button).Name;
            this.DialogResult = true; // Close the dialog with a "true" result
            this.Close(); // Ensure the dialog closes
        }
    }
}

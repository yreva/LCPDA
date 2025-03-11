using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Interaction logic for LimitMassRangeView.xaml
    /// </summary>
    public partial class LimitMassRangeView : Window
    {
        public LimitMassRangeView()
        {
            InitializeComponent();
        }

        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.MassRangeLimitEnabled = LimitEnabled.IsChecked.GetValueOrDefault();

        }
        private void EntryBox_KeyDown(object sender, KeyEventArgs e)
        {

            double newValue = 0;
            if (e.Key == Key.Enter)
            {

                if (double.TryParse((sender as TextBox).Text, out double result))
                {
                    (sender as TextBox).Text = result.ToString(CultureInfo.InvariantCulture); // Ensure consistent formatting
                    newValue = result;

                    switch ((sender as TextBox).Name)
                    {
                        case "MassMin":
                            PlotSettings.Instance.MassRangeMinimum = newValue;
                            break;
                        case "MassMax":
                            PlotSettings.Instance.MassRangeMaximum = newValue;
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid input. Please enter a valid number.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                FocusManager.SetFocusedElement(this, null);
            }
        }

        private void EntryBox_LostFocus(object sender, RoutedEventArgs e)
        {
            double newValue = 0;
            if (double.TryParse((sender as TextBox).Text, out double result))
            {
                (sender as TextBox).Text = result.ToString(CultureInfo.InvariantCulture); // Ensure consistent formatting
                newValue = result;

                switch ((sender as TextBox).Name)
                {
                    case "MassMin":
                        PlotSettings.Instance.MassRangeMinimum = newValue;
                        break;
                    case "MassMax":
                        PlotSettings.Instance.MassRangeMaximum = newValue;
                        break;
                }
            }
        }
    }
}

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
using ScottPlot;

namespace RVMS.Views
{
    /// <summary>
    /// Interaction logic for LimitMassRangeView.xaml
    /// </summary>
    public partial class LimitMassRangeView : Window
    {
        public LimitMassRangeView()
        {
            InitializeComponent();
            if (PlotSettings.Instance.WavelengthRangeLimitEnabled)
            {
                MassMin.Text = Math.Round(PlotSettings.Instance.WavelengthRangeMinimum, 2).ToString();
                MassMax.Text = Math.Round(PlotSettings.Instance.WavelengthRangeMaximum, 2).ToString();
                MassMin.IsEnabled = true;
                MassMax.IsEnabled = true;
            }
        }

        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            return;
            //PlotSettings.Instance.WavelengthRangeLimitEnabled = LimitEnabled.IsChecked.GetValueOrDefault();
            if (PlotSettings.Instance.WavelengthRangeLimitEnabled)
            {
                MassMin.Text = Math.Round(PlotSettings.Instance.WavelengthRangeMinimum, 2).ToString();
                MassMax.Text = Math.Round(PlotSettings.Instance.WavelengthRangeMaximum, 2).ToString();
            }
            else
            {
                MassMin.Text = "";
                MassMin.IsEnabled = false;
                MassMax.Text = "";
                MassMax.IsEnabled = false;
            }
        }
        private void EntryBox_KeyDown(object sender, KeyEventArgs e)
        {
            return;
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
                            PlotSettings.Instance.WavelengthRangeMinimum = newValue;
                            break;
                        case "MassMax":
                            PlotSettings.Instance.WavelengthRangeMaximum = newValue;
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
            return;
            double newValue = 0;
            if (double.TryParse((sender as TextBox).Text, out double result))
            {
                (sender as TextBox).Text = result.ToString(CultureInfo.InvariantCulture); // Ensure consistent formatting
                newValue = result;

                switch ((sender as TextBox).Name)
                {
                    case "MassMin":
                        PlotSettings.Instance.WavelengthRangeMinimum = newValue;
                        break;
                    case "MassMax":
                        PlotSettings.Instance.WavelengthRangeMaximum = newValue;
                        break;
                }
            }
        }

        private void OnClick_CloseAndApplyRange(object sender, RoutedEventArgs e)
        {
            double.TryParse(MassMin.Text, out double minVal);
            double.TryParse(MassMax.Text, out double maxVal);

            // return if min wav is not a number
            if (minVal == null)
            {
                MessageBox.Show("Invalid input for minimum wavelength. Please enter a valid number.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // return if max wav is not a number
            if (maxVal == null)
            {
                MessageBox.Show("Invalid input for maximum wavelength. Please enter a valid number.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // return if min wav is larger than max wav
            if (minVal > maxVal)
            {
                MessageBox.Show("Maximum wavelength should be greater than minimum wavelength.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // return if both values appear to be zero
            if (maxVal == 0 && minVal == 0)
            {
                MessageBox.Show("Error parsing limit values.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // if here, things look good
            PlotSettings.Instance.WavelengthRangeMinimum = minVal;
            PlotSettings.Instance.WavelengthRangeMaximum = maxVal;
            PlotSettings.Instance.WavelengthRangeLimitEnabled = true;
            this.Close();

        }

        private void OnClick_CloseAndAutoscale(object sender, RoutedEventArgs e)
        {

            PlotSettings.Instance.WavelengthRangeLimitEnabled = false;
            this.Close();
        }
    }
}

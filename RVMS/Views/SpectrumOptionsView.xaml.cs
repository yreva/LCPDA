using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

namespace RVMS.Views
{
    /// <summary>
    /// Interaction logic for SpectrumOptionsView.xaml
    /// </summary>
    public partial class SpectrumOptionsView : Window
    {
        private int previous_AutoX;
        private int previous_AutoY;
        private double previous_XMin;
        private double previous_XMax;
        private double previous_YMin;
        private double previous_YMax;

        private bool previous_HoldManual;
        private bool previous_PlotGrid;
        private bool previous_ScrollEnabled;
        private bool previous_ShowImportedSpectrum;

        private ScottPlot.Color previous_Color;

        public SpectrumOptionsView()
        {
            InitializeComponent();
            LoadColors();

            previous_XMin = PlotSettings.Instance.Spectrum.XMin;
            previous_XMax = PlotSettings.Instance.Spectrum.XMax;
            previous_YMin = PlotSettings.Instance.Spectrum.YMin;
            previous_YMax = PlotSettings.Instance.Spectrum.YMax;
            previous_Color = PlotSettings.Instance.Spectrum.LineColor;
            previous_HoldManual = PlotSettings.Instance.Spectrum.HoldManualLimits;
            previous_PlotGrid = PlotSettings.Instance.Spectrum.GridEnabled;
            previous_ScrollEnabled = PlotSettings.Instance.Spectrum.MouseEventsEnabled;
            previous_ShowImportedSpectrum = PlotSettings.Instance.Spectrum.ShowImportedSpectrum;


            XMin.Text = Math.Round(previous_XMin,1).ToString();
            YMin.Text = Math.Round(previous_YMin,2).ToString();
            XMax.Text = Math.Round(previous_XMax,1).ToString();
            YMax.Text = Math.Round(previous_YMax,2).ToString();
            PlotGrid.IsChecked = PlotSettings.Instance.Spectrum.GridEnabled;
            ScrollEnabled.IsChecked = PlotSettings.Instance.Spectrum.MouseEventsEnabled;
            HoldLimits.IsChecked = PlotSettings.Instance.Spectrum.HoldManualLimits;

            if (!PlotSettings.Instance.Spectrum.HasSpectrumBeenImported())
            {
                return;
            }

            this.Height = 390;
            ImportedDivider.Visibility = Visibility.Visible;
            ImportedCheckBox.Visibility = Visibility.Visible;
            ImportedCheckBox.IsChecked = PlotSettings.Instance.Spectrum.ShowImportedSpectrum;

            if (PlotSettings.Instance.Spectrum.ShowImportedSpectrum)
            {
                this.Height = 500;
                ImportedDivider.Visibility = Visibility.Visible;
                ImportedHeader.Visibility = Visibility.Visible;
                ImportedComboBox.Visibility = Visibility.Visible;
                ImportedScalerHeader.Visibility = Visibility.Visible;
                ImportedScaler.Visibility = Visibility.Visible;
            }
        }

        public void UpdateLayout()
        {
            if (!PlotSettings.Instance.Spectrum.HasSpectrumBeenImported())
            {
                return;
            }

            this.Height = 390;
            ImportedDivider.Visibility = Visibility.Visible;
            ImportedCheckBox.Visibility = Visibility.Visible;
            ImportedCheckBox.IsChecked = PlotSettings.Instance.Spectrum.ShowImportedSpectrum;

            if (PlotSettings.Instance.Spectrum.ShowImportedSpectrum)
            {
                this.Height = 500;
                ImportedDivider.Visibility = Visibility.Visible;
                ImportedHeader.Visibility = Visibility.Visible;
                ImportedComboBox.Visibility = Visibility.Visible;
                ImportedScalerHeader.Visibility = Visibility.Visible;
                ImportedScaler.Visibility = Visibility.Visible;
            }

            this.Top = Application.Current.MainWindow.Top + Application.Current.MainWindow.Height - this.Height;
            this.Left = Application.Current.MainWindow.Left + Application.Current.MainWindow.Width;
        }

        private void OnClick_SaveSettings(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnClick_CloseNoSave(object sender, RoutedEventArgs e)
        {

            PlotSettings.Instance.Spectrum.AutoScaleX = previous_AutoX;
            PlotSettings.Instance.Spectrum.AutoScaleY = previous_AutoY;
            PlotSettings.Instance.Spectrum.XMin = previous_XMin;
            PlotSettings.Instance.Spectrum.XMax = previous_XMax;
            PlotSettings.Instance.Spectrum.YMin = previous_YMin;
            PlotSettings.Instance.Spectrum.YMax = previous_YMax;

            PlotSettings.Instance.Spectrum.LineColor = previous_Color;
            PlotSettings.Instance.Spectrum.GridEnabled = previous_PlotGrid;
            PlotSettings.Instance.Spectrum.MouseEventsEnabled = previous_ScrollEnabled;
            PlotSettings.Instance.Spectrum.HoldManualLimits = previous_HoldManual;

            this.Close();
        }

        private void OnClick_AutoX(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Spectrum.AutoScaleX += 1;
            XMin.Text = Math.Round(PlotSettings.Instance.Spectrum.XMin, 2).ToString();
            XMax.Text = Math.Round(PlotSettings.Instance.Spectrum.XMax, 2).ToString();

        }

        private void OnClick_AutoY(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Spectrum.AutoScaleY += 1;
            YMin.Text = Math.Round(PlotSettings.Instance.Spectrum.YMin, 2).ToString();
            YMax.Text = Math.Round(PlotSettings.Instance.Spectrum.YMax, 2).ToString();
        }


        private void OnChecked_ShowGrid(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Spectrum.GridEnabled = (sender as CheckBox).IsChecked.GetValueOrDefault();

        }

        private void OnChecked_MouseEvents(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                PlotSettings.Instance.Spectrum.MouseEventsEnabled = true;
            }
            else
            {
                PlotSettings.Instance.Spectrum.MouseEventsEnabled = false;
            }
        }

        private void OnClick_HoldLimits(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                PlotSettings.Instance.Spectrum.HoldManualLimits = true;
            }
            else
            {
                PlotSettings.Instance.Spectrum.HoldManualLimits = false;
            }
        }

        private void OnClick_ShowImportedSpectrum(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                this.Height = 485;
                PlotSettings.Instance.Spectrum.ShowImportedSpectrum = true; 
                ImportedDivider.Visibility = Visibility.Visible;
                ImportedCheckBox.Visibility = Visibility.Visible;
                ImportedHeader.Visibility = Visibility.Visible;
                ImportedComboBox.Visibility = Visibility.Visible;
                ImportedScalerHeader.Visibility = Visibility.Visible;
                ImportedScaler.Visibility = Visibility.Visible;
            }
            else
            {
                this.Height = 415;
                PlotSettings.Instance.Spectrum.ShowImportedSpectrum = false;
                ImportedCheckBox.Visibility = Visibility.Visible;
                ImportedHeader.Visibility = Visibility.Collapsed;
                ImportedComboBox.Visibility = Visibility.Collapsed;
                ImportedScalerHeader.Visibility = Visibility.Collapsed;
                ImportedScaler.Visibility = Visibility.Collapsed;

            }
        }

        private void ImportedScaler_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double increment = 0.1;  // Adjust increment as needed
            double.TryParse(ImportedScaler.Text, out double value);
            double newValue = value + (e.Delta > 0 ? increment : -increment);

            if (newValue <= 0.1)
            {
                newValue = 0.1;
            }

            ImportedScaler.Text = newValue.ToString("0.00");
            PlotSettings.Instance.Spectrum.ImportedSpectrumScaler = newValue;
        }

        private void ImportedScaler_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
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
                        case "XMin":
                            PlotSettings.Instance.Spectrum.XMin = newValue;
                            break;
                        case "XMax":
                            PlotSettings.Instance.Spectrum.XMax = newValue;
                            break;
                        case "YMin":
                            PlotSettings.Instance.Spectrum.YMin = newValue;
                            break;
                        case "YMax":
                            PlotSettings.Instance.Spectrum.YMax = newValue;
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

        private void LoadColors()
        {
            // Get all standard colors from System.Drawing.Color
            var colors = typeof(System.Drawing.Color).GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(p => p.PropertyType == typeof(System.Drawing.Color))
                .Select(p => p.GetValue(null))
                .Cast<System.Drawing.Color>()
                .ToList();

            var x = PlotSettings.Instance.Spectrum.LineColor.ToSDColor();
            var y = PlotSettings.Instance.Spectrum.ImportedLineColor.ToSDColor();

            LineColorComboBox.ItemsSource = colors;
            LineColorComboBox.SelectedItem = colors.First(c => c.ToArgb() == x.ToArgb());

            ImportedComboBox.ItemsSource = colors;
            ImportedComboBox.SelectedItem = colors.First(c => c.ToArgb() == y.ToArgb());
        }

        private void ColorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch ((sender as ComboBox).Name)
            {
                case "ImportedLineColor":
                    PlotSettings.Instance.Spectrum.ImportedLineColor = ScottPlot.Color.FromSDColor((System.Drawing.Color)ImportedComboBox.SelectedItem);
                    break;
                case "RawDataLineColor":
                    PlotSettings.Instance.Spectrum.LineColor = ScottPlot.Color.FromSDColor((System.Drawing.Color)LineColorComboBox.SelectedItem);
                    break;
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
                    case "XMin":
                        PlotSettings.Instance.Spectrum.XMin = newValue;
                        break;
                    case "XMax":
                        PlotSettings.Instance.Spectrum.XMax = newValue;
                        break;
                    case "YMin":
                        PlotSettings.Instance.Spectrum.YMin = newValue;
                        break;
                    case "YMax":
                        PlotSettings.Instance.Spectrum.YMax = newValue;
                        break;
                }
            }
        }
    }

}

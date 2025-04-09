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

namespace RawVision.Views
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

        private ScottPlot.Color previous_Color;

        public SpectrumOptionsView()
        {
            InitializeComponent();
            LoadColors();

            previous_XMin = PlotSettings.Instance.Spectrum.XMin;
            previous_XMax = PlotSettings.Instance.Spectrum.XMax;
            previous_YMin = PlotSettings.Instance.Spectrum.YMin;
            previous_YMax = PlotSettings.Instance.Spectrum.YMax;
            previous_Color = PlotSettings.Instance.Spectrum.BarColor;
            previous_HoldManual = PlotSettings.Instance.Spectrum.HoldManualLimits;
            previous_PlotGrid = PlotSettings.Instance.Spectrum.GridEnabled;
            previous_ScrollEnabled = PlotSettings.Instance.Spectrum.MouseEventsEnabled;


            XMin.Text = Math.Round(previous_XMin, 2).ToString();
            YMin.Text = Math.Round(previous_YMin, 2).ToString();
            XMax.Text = Math.Round(previous_XMax, 2).ToString();
            YMax.Text = Math.Round(previous_YMax, 2).ToString();
            PlotGrid.IsChecked = PlotSettings.Instance.Spectrum.GridEnabled;
            ScrollEnabled.IsChecked = PlotSettings.Instance.Spectrum.MouseEventsEnabled;
            HoldLimits.IsChecked = PlotSettings.Instance.Spectrum.HoldManualLimits;
            ColorComboBox.SelectedItem = PlotSettings.Instance.Spectrum.BarColor;
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

            PlotSettings.Instance.Spectrum.BarColor = previous_Color;
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

            var x = (ColorComboBox as ComboBox);
            var y = PlotSettings.Instance.Spectrum.BarColor.ToSDColor();

            ColorComboBox.ItemsSource = colors;
            ColorComboBox.SelectedItem = colors.First(c => c.ToArgb() == y.ToArgb());
        }

        private void ColorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ColorComboBox.SelectedItem is System.Drawing.Color selectedColor)
            {
                PlotSettings.Instance.Spectrum.BarColor = ScottPlot.Color.FromSDColor(selectedColor);
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

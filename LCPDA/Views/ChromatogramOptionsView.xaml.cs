using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RawVision.ViewModels;
using ScottPlot;
using ScottPlot.DataViews;
using Color = System.Drawing.Color;

namespace RawVision.Views
{
    /// <summary>
    /// Interaction logic for ChromatogramOptionsView.xaml
    /// </summary>
    public partial class ChromatogramOptionsView : Window
    {
        private int AutoXSetting;
        private int AutoYSetting;
        private double XMinSetting;
        private double XMaxSetting;
        private double YMinSetting;
        private double YMaxSetting;

        private ScottPlot.Color Previous_Color;

        public ChromatogramOptionsView()
        {
            InitializeComponent();
            LoadColors();
            AutoXSetting = PlotSettings.Instance.Chromatogram.AutoScaleX;
            AutoYSetting = PlotSettings.Instance.Chromatogram.AutoScaleY;
            XMinSetting = PlotSettings.Instance.Chromatogram.XMin;
            XMaxSetting = PlotSettings.Instance.Chromatogram.XMax;
            YMinSetting = PlotSettings.Instance.Chromatogram.YMin;
            YMaxSetting = PlotSettings.Instance.Chromatogram.YMax;
            XMin.Text = Math.Round(XMinSetting,2).ToString();
            YMin.Text = Math.Round(YMinSetting,2).ToString();
            XMax.Text = Math.Round(XMaxSetting,2).ToString();
            YMax.Text = Math.Round(YMaxSetting,2).ToString();

            VLine.IsChecked = PlotSettings.Instance.Chromatogram.VLineEnabled;
            PlotGrid.IsChecked = PlotSettings.Instance.Chromatogram.GridEnabled;
            ScrollEnabled.IsChecked = PlotSettings.Instance.Chromatogram.MouseEventsEnabled;

            Previous_Color = PlotSettings.Instance.Chromatogram.LineColor;
        }

        private void OnClick_SaveSettings(object sender, RoutedEventArgs e)
        {
            //
            this.Close();
        }

        private void OnClick_CloseNoSave(object sender, RoutedEventArgs e)
        {

            PlotSettings.Instance.Chromatogram.AutoScaleX = AutoXSetting;
            PlotSettings.Instance.Chromatogram.AutoScaleY = AutoYSetting;
            PlotSettings.Instance.Chromatogram.XMin = XMinSetting;
            PlotSettings.Instance.Chromatogram.XMax = XMaxSetting;
            PlotSettings.Instance.Chromatogram.YMin = YMinSetting;
            PlotSettings.Instance.Chromatogram.YMax = YMaxSetting;
            PlotSettings.Instance.Chromatogram.LineColor = Previous_Color;
            this.Close();
        }

        private void OnClick_AutoX(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.AutoScaleX += 1;
            XMin.Text = Math.Round(XMinSetting, 2).ToString();
            XMax.Text = Math.Round(XMaxSetting, 2).ToString();

        }

        private void OnClick_AutoY(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.AutoScaleY += 1;
            YMin.Text = Math.Round(YMinSetting, 2).ToString();
            YMax.Text = Math.Round(YMaxSetting, 2).ToString();
        }

        private void OnChecked_ShowVLine(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.VLineEnabled = (sender as CheckBox).IsChecked.Value;
        }

        private void OnChecked_ShowGrid(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.GridEnabled = (sender as CheckBox).IsChecked.Value;

        }

        private void OnChecked_MouseEvents(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                PlotSettings.Instance.Chromatogram.MouseEventsEnabled = true;
            }
            else
            {
                PlotSettings.Instance.Chromatogram.MouseEventsEnabled = false;
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
                            PlotSettings.Instance.Chromatogram.XMin = newValue;
                            break;
                        case "XMax":
                            PlotSettings.Instance.Chromatogram.XMax = newValue;
                            break;
                        case "YMin":
                            PlotSettings.Instance.Chromatogram.YMin = newValue;
                            break;
                        case "YMax":
                            PlotSettings.Instance.Chromatogram.YMax = newValue;
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
            var y = PlotSettings.Instance.Chromatogram.LineColor.ToSDColor();

            ColorComboBox.ItemsSource = colors;
            ColorComboBox.SelectedItem = colors.First(c => c.ToArgb() == y.ToArgb());
        }

        private void ColorComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ColorComboBox.SelectedItem is System.Drawing.Color selectedColor)
            {
                PlotSettings.Instance.Chromatogram.LineColor = ScottPlot.Color.FromSDColor(selectedColor);
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
                        PlotSettings.Instance.Chromatogram.XMin = newValue;
                        break;
                    case "XMax":
                        PlotSettings.Instance.Chromatogram.XMax = newValue;
                        break;
                    case "YMin":
                        PlotSettings.Instance.Chromatogram.YMin = newValue;
                        break;
                    case "YMax":
                        PlotSettings.Instance.Chromatogram.YMax = newValue;
                        break;
                }
            }
        }
    }
}

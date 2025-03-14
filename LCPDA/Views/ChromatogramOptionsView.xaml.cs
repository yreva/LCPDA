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
        private int previous_AutoX;
        private int previous_AutoY;
        private double previous_XMin;
        private double previous_XMax;
        private double previous_YMin;
        private double previous_YMax;
        private double previous_ColorMin;
        private double previous_ColorMax;

        private bool previous_Vline;
        private bool previous_PlotGrid;
        private bool previous_ScrollEnabled;

        private ScottPlot.Color previous_Color;

        public ChromatogramOptionsView()
        {
            InitializeComponent();
            LoadColors();
            previous_AutoX = PlotSettings.Instance.Chromatogram.AutoScaleX;
            previous_AutoY = PlotSettings.Instance.Chromatogram.AutoScaleY;
            previous_XMin = PlotSettings.Instance.Chromatogram.XMin;
            previous_XMax = PlotSettings.Instance.Chromatogram.XMax;
            previous_YMin = PlotSettings.Instance.Chromatogram.YMin;
            previous_YMax = PlotSettings.Instance.Chromatogram.YMax;
            XMin.Text = Math.Round(previous_XMin,2).ToString();
            YMin.Text = Math.Round(previous_YMin,2).ToString();
            XMax.Text = Math.Round(previous_XMax,2).ToString();
            YMax.Text = Math.Round(previous_YMax,2).ToString();

            VLine.IsChecked = PlotSettings.Instance.Chromatogram.VLineEnabled;
            PlotGrid.IsChecked = PlotSettings.Instance.Chromatogram.GridEnabled;
            ScrollEnabled.IsChecked = PlotSettings.Instance.Chromatogram.MouseEventsEnabled;

            previous_Color = PlotSettings.Instance.Chromatogram.LineColor;
            previous_Vline = PlotSettings.Instance.Chromatogram.VLineEnabled;
            previous_PlotGrid = PlotSettings.Instance.Chromatogram.GridEnabled;
            previous_ScrollEnabled = PlotSettings.Instance.Chromatogram.MouseEventsEnabled;

            if (!double.IsNaN(PlotSettings.Instance.Chromatogram.ColorMin))
            {
                ColorMin.Text = PlotSettings.Instance.Chromatogram.ColorMin.ToString();
                previous_ColorMin = PlotSettings.Instance.Chromatogram.ColorMin;
            }
            if (!double.IsNaN(PlotSettings.Instance.Chromatogram.ColorMax))
            {
                ColorMax.Text = PlotSettings.Instance.Chromatogram.ColorMax.ToString();
                previous_ColorMax = PlotSettings.Instance.Chromatogram.ColorMax;

            }
        }

        private void OnClick_SaveSettings(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnClick_CloseNoSave(object sender, RoutedEventArgs e)
        {

            PlotSettings.Instance.Chromatogram.AutoScaleX = previous_AutoX;
            PlotSettings.Instance.Chromatogram.AutoScaleY = previous_AutoY;
            PlotSettings.Instance.Chromatogram.XMin = previous_XMin;
            PlotSettings.Instance.Chromatogram.XMax = previous_XMax;
            PlotSettings.Instance.Chromatogram.YMin = previous_YMin;
            PlotSettings.Instance.Chromatogram.YMax = previous_YMax;
            PlotSettings.Instance.Chromatogram.ColorMin = previous_ColorMin;
            PlotSettings.Instance.Chromatogram.ColorMax = previous_ColorMax;
            PlotSettings.Instance.Chromatogram.LineColor = previous_Color;
            PlotSettings.Instance.Chromatogram.VLineEnabled = previous_Vline;
            PlotSettings.Instance.Chromatogram.GridEnabled = previous_PlotGrid;
            PlotSettings.Instance.Chromatogram.MouseEventsEnabled = previous_ScrollEnabled;

            this.Close();
        }

        private void OnClick_AutoX(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.AutoScaleX += 1;
            XMin.Text = Math.Round(previous_XMin, 2).ToString();
            XMax.Text = Math.Round(previous_XMax, 2).ToString();

        }

        private void OnClick_AutoY(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.AutoScaleY += 1;
            YMin.Text = Math.Round(previous_YMin, 2).ToString();
            YMax.Text = Math.Round(previous_YMax, 2).ToString();
        }

        private void OnChecked_ShowVLine(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.VLineEnabled = (sender as CheckBox).IsChecked.GetValueOrDefault();
        }

        private void OnChecked_ShowGrid(object sender, RoutedEventArgs e)
        {
            PlotSettings.Instance.Chromatogram.GridEnabled = (sender as CheckBox).IsChecked.GetValueOrDefault();

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
                        case "ColorMin":
                            PlotSettings.Instance.Chromatogram.ColorMin = newValue;
                            break;
                        case "ColorMax":
                            PlotSettings.Instance.Chromatogram.ColorMax = newValue;
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
                    case "ColorMin":
                        PlotSettings.Instance.Chromatogram.ColorMin = newValue;
                        break;
                    case "ColorMax":
                        PlotSettings.Instance.Chromatogram.ColorMax = newValue;
                        break;
                }
            }
        }
    }
}

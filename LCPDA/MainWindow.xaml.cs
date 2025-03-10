using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RawVision.ViewModels;
using ScottPlot.WPF;
using ThermoFisher.CommonCore.Data.Business;

namespace RawVision.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            // Set the DataContext to the MainViewModel
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            ChromatogramContainer.Children.Add(viewModel.ChromatogramPlot);
            SpectrumContainer.Children.Add(viewModel.SpectrumPlot);
            viewModel.ChromatogramPlot.PreviewKeyDown += WpfPlot_KeyDown;
            viewModel.SpectrumPlot.PreviewKeyDown += WpfPlot_KeyDown;

        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                // Move to a parent that can take focus
                FrameworkElement parent = (FrameworkElement)(textBox).Parent;
                while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
                {
                    parent = (FrameworkElement)parent.Parent;
                }

                DependencyObject scope = FocusManager.GetFocusScope(textBox);
                FocusManager.SetFocusedElement(scope, parent as IInputElement);
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            ChromatogramContainer.Children.Add(viewModel.ChromatogramPlot);
            SpectrumContainer.Children.Add(viewModel.SpectrumPlot);
        }

        public void IncrementScanNumber(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;

            var x = (sender as Button);
            var option = x.Content as string;

            switch (option)
            {
                case "+ 10 Scans":
                    DC.IncrementScan(10);
                    break;
                case "+ 1 Scan":
                    DC.IncrementScan(1);
                    break;
                case "- 1 Scan":
                    DC.IncrementScan(-1);
                    break;
                case "- 10 Scans":
                    DC.IncrementScan(-10);
                    break;
            }
        }

        private void WpfPlot_KeyDown(object sender, KeyEventArgs e)
        {
            // Suppress arrow key interactions
            if (e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true; // Prevent default action
            }

            switch (e.Key)
            {
                case Key.Right:
                    (DataContext as MainViewModel).IncrementScan(1);
                    break;
                case Key.Left:
                    (DataContext as MainViewModel).IncrementScan(-1);
                    break;

            }
        }

        public void ChangeMassResolution(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;
            ResolutionDialogue dialog = new ResolutionDialogue(DC.MassResolutionDecimal);
            if (dialog.ShowDialog() == true)
            {
                DC.MassResolutionDecimal = dialog.SelectedNumber;
                DC.MassResolutionChanged();
            }
        }

        public void ChangeMapScaling(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;
            ScalingDialogue dialog = new ScalingDialogue(PlotSettings.Instance.Chromatogram.MapScaling);
            if (dialog.ShowDialog() == true)
            {
                PlotSettings.Instance.Chromatogram.MapScaling = dialog.SelectedScalingMethod;
                DC.ScalingMethodChanged();
            }
        }

        public void ChangeColormap(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;
            ColormapSettingsDialogue dialog = new ColormapSettingsDialogue(DC.GetColormapSetting());
            if (dialog.ShowDialog() == true)
            {
                DC.SetColormapSetting(dialog.SelectColormapName);
                DC.ScalingMethodChanged();
            }
        }

        public void About_Click(object sender, RoutedEventArgs e)
        {
            About aboutWindow = new About();
            if (aboutWindow.ShowDialog() == true)
            {
                //
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using RawVision.ViewModels;
using RawVision.Views;
using ScottPlot;
using ScottPlot.Panels;
using ScottPlot.WPF;

namespace RawVision.Models
{
    public class PlotModel : INotifyPropertyChanged
    {
        //
        // Implement PropertyChanged methods
        //
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // vars for plots from UI
        private WpfPlot _chromatogramPlot;
        private WpfPlot _spectrumPlot;

        private ScottPlot.Panels.ColorBar _colorbar;

        private ChromatogramViewModel _chromatogramViewModel;
        private SpectrumViewModel _spectrumViewModel;

        public PlotModel(WpfPlot cp, WpfPlot sp, ChromatogramViewModel cvm, SpectrumViewModel svm)
        {
            _chromatogramPlot = cp;
            _spectrumPlot = sp;
            _chromatogramViewModel = cvm;
            _spectrumViewModel = svm;

            PlotSettings.Instance.PropertyChanged += PlotSettings_PropertyChanged;

            SetColormapByName("Ice", false);

            // subscribe to plot events
            _chromatogramPlot.MouseDown += ChromPlot_MouseDown;
            _chromatogramPlot.MouseDoubleClick += ChromPlot_MouseDoubleClick;

            //DisablePlotBenchmarking();
        }

        private string _chromatogramStyle;
        public string ChromatogramStyle
        {
            get { return _chromatogramStyle; }
            set
            {
                _chromatogramStyle = value;
                OnPropertyChanged(nameof(ChromatogramStyle));
            }
        }

        private IColormap _colormap;

        public IColormap Colormap
        {
            get { return _colormap; }
            set
            {
                _colormap = value;
            }
        }

        public void PlotChromatogram()
        {
            var plt = _chromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("Intensity");

            var scatter = plt.Add.ScatterLine(x, y);
            scatter.LineWidth = 1.5F;

            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

            if (_colorbar != null)
            {
                _colorbar.IsVisible = false;
            }

            _chromatogramPlot.Refresh();
        }

        public void Plot2DChromatogram(string style)
        {
            _chromatogramStyle = style;
            if (style == "Linear")
            {
                PlotLinearMzMap();
            }

            else if (style == "Log10")
            {
                PlotLog10MzMap();
            }
        }

        private void PlotLinearMzMap()
        {
            var plt = _chromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("m/z");

            double[,] flippedData = FlipVertically(_spectrumViewModel.Intensities2D);

            var hm = plt.Add.Heatmap(_spectrumViewModel.Intensities2D);

            hm.Colormap = Colormap;
            hm.Smooth = true;

            if (_colorbar == null)
            {
                _colorbar = plt.Add.ColorBar(hm);
            }
            else
            {
                _colorbar.Source = hm;
            }

            _colorbar.IsVisible = true;
            _colorbar.Label = "Intensity";

            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

            _chromatogramPlot.Refresh();
        }

        private void PlotLog10MzMap()
        {
            var plt = _chromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("m/z");

            double[,] flippedData = FlipVertically(_spectrumViewModel.Log10Intensities2D);

            var hm = plt.Add.Heatmap(flippedData);

            hm.Colormap = Colormap;
            hm.Smooth = true;

            if (_colorbar == null)
            {
                _colorbar = plt.Add.ColorBar(hm);
            }
            else
            {
                _colorbar.Source = hm;
            }

            _colorbar.IsVisible = true;
            _colorbar.Label = "Log(Intensity)";

            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

            _chromatogramPlot.Refresh();
        }

        private double[,] FlipVertically(double[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            double[,] newArray = new double[rows, cols];

            for (int i = 0; i < rows / 2; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    // Swap element at (i, j) with (rows - i - 1, j)
                    newArray[rows - i - 1, j] = array[i, j];
                }
            }

            return newArray;
        }

        private void ResetVlineOnChomatogram()
        {
            var plt = _chromatogramPlot.Plot;
            var vline = plt.PlottableList.FirstOrDefault(x => x.ToString().Contains("VerticalLine"));
            plt.PlottableList.Remove(vline);

            double lineLoc = (_chromatogramStyle == "Line") ? _chromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1] : PlotSettings.Instance.ScanNumber;

            var line = plt.Add.VerticalLine(lineLoc);
            line.LineWidth = 1;
            line.Color = ScottPlot.Color.FromHex("#0f0f0f");
            _chromatogramPlot.Refresh();
        }

        public void PlotMassSpectrum()
        {
            if (_spectrumViewModel.MZ.Count() == 0)
            {
                return;
            }

            var plt = _spectrumPlot.Plot;

            plt.Clear();

            var x = _spectrumViewModel.MZ[PlotSettings.Instance.ScanNumber - 1];
            var y = _spectrumViewModel.Intensity[PlotSettings.Instance.ScanNumber - 1];

            plt.Add.Bars(x, y);

            plt.XLabel("m/z");
            plt.YLabel("Intensity");

            plt.Axes.AutoScale();
            plt.Axes.AntiAlias(true);

            _spectrumPlot.Refresh();
        }

        public int GetScanNumberFromRetentionTime(double rt)
        {
            int x = Array.FindIndex(_chromatogramViewModel.Times, t => t == rt);
            return x + 1;
        }

        //private void ChromPlot_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (_chromatogramViewModel.Times == null)
        //    {
        //        return;
        //    }
        //    if (e.LeftButton == MouseButtonState.Pressed)
        //    {
        //        var mouse = e.GetPosition(_chromatogramPlot);
        //        var x = mouse.X;
        //        var y = mouse.Y;
        //        double clickedX = _chromatogramPlot.Plot.GetCoordinates(new Pixel(x, y)).X;

        //        if (ChromatogramStyle == "Line")
        //        {
        //            // Find the closest time point
        //            double nearestTime = _chromatogramViewModel.Times.OrderBy(x => Math.Abs(x - clickedX)).FirstOrDefault();
        //            // Update ViewModel
        //            ScanNumber = GetScanNumberFromRetentionTime(nearestTime);
        //        }
        //        else
        //        {
        //            ScanNumber = (int)Math.Abs(clickedX);
        //        }
        //    }
        //}

        private void PlotSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ScanNumber":
                    PlotMassSpectrum();
                    ResetVlineOnChomatogram();
                    break;

            }
        }


        private void ChromPlot_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var window = Application.Current.Windows.OfType<ChromatogramOptionsView>().FirstOrDefault();

            if (window == null)
            {
                ChromatogramOptionsView view = new ChromatogramOptionsView();
                view.Show();
            }
            else
            {
                window.WindowState = WindowState.Normal;
                window.Activate();
                window.Topmost = true;
                window.Topmost = false;
            }
            
            e.Handled = true;
        }

        private void ChromPlot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_chromatogramViewModel.Times == null)
            {
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mouse = e.GetPosition(_chromatogramPlot);
                var x = mouse.X;
                var y = mouse.Y;
                double clickedX = _chromatogramPlot.Plot.GetCoordinates(new Pixel(x, y)).X;

                if (_chromatogramStyle == "Line")
                {
                    // Find the closest time point
                    double nearestTime = _chromatogramViewModel.Times.OrderBy(x => Math.Abs(x - clickedX)).FirstOrDefault();
                    // Update ViewModel
                    PlotSettings.Instance.ScanNumber = GetScanNumberFromRetentionTime(nearestTime);
                }
                else
                {
                    PlotSettings.Instance.ScanNumber = (int)Math.Abs(clickedX);
                }

            }
        }

        public void SetColormapByName(string colormapName, bool reverse)
        {
            IColormap cm = null;
            // have to do switch because ScottPlot; could switch to more dynamic colormap library in future
            switch (colormapName)
            {
                case "Algae":
                    cm = new ScottPlot.Colormaps.Algae();
                    break;
                case "Blues":
                    cm = new ScottPlot.Colormaps.Blues();
                    break;
                case "Deep":
                    cm = new ScottPlot.Colormaps.Deep();
                    break;
                case "Dense":
                    cm = new ScottPlot.Colormaps.Dense();
                    break;
                case "Ice":
                    cm = new ScottPlot.Colormaps.Ice();
                    break;
                case "Grayscale":
                    cm = new ScottPlot.Colormaps.Grayscale();
                    break;
                case "Plasma":
                    cm = new ScottPlot.Colormaps.Plasma();
                    break;
                case "Solar":
                    cm = new ScottPlot.Colormaps.Solar();
                    break;
                case "Thermal":
                    cm = new ScottPlot.Colormaps.Thermal();
                    break;
                case "Turbo":
                    cm = new ScottPlot.Colormaps.Turbo();
                    break;
            }

            if (reverse)
            {
                Colormap = cm.Reversed();
            }
            else
            {
                Colormap = cm;
            }
        }
    }
}

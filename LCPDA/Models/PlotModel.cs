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
using ScottPlot.Plottables;
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


        // PlotSettings changed; A lot of stuff firing off from here.
        private void PlotSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ScanNumber":
                    PlotMassSpectrum();
                    ResetVlineOnChomatogram();
                    break;

                case "AutoScaleX":
                    ResetScalingX(true);
                    _chromatogramPlot.Refresh();
                    break;

                case "AutoScaleY":
                    ResetScalingY(true);
                    break;
                case "XMin":
                case "XMax":
                    SetManualLimits("X");
                    break;

                case "YMin":
                case "YMax":
                    SetManualLimits("Y");
                    break;

                case "LineColor":
                    ResetLineColor();
                    break;

                case "VLineEnabled":
                    VLineSettingChanged();
                    break;

                case "GridEnabled":
                    GridSettingChanged();
                    break;

                case "MouseEventsEnabled":
                    MouseEventSettingChanged();
                    break;


            }
        }

        private void VLineSettingChanged()
        {
            //
            var plt = _chromatogramPlot.Plot;
            var vline = plt.PlottableList.FirstOrDefault(x => x.ToString().Contains("VerticalLine"));
            vline.IsVisible = PlotSettings.Instance.Chromatogram.VLineEnabled;
            _chromatogramPlot.Refresh();
        }

        private void GridSettingChanged()
        {
            if (PlotSettings.Instance.Chromatogram.GridEnabled)
            {
                _chromatogramPlot.Plot.ShowGrid();
            }
            else
            {
                _chromatogramPlot.Plot.HideGrid();
            }
            _chromatogramPlot.Refresh();
        }

        private bool _hasMouseBeenDisabled = false;
        private void MouseEventSettingChanged()
        {
            if (PlotSettings.Instance.Chromatogram.MouseEventsEnabled)
            {
                if (_hasMouseBeenDisabled)
                {
                    _chromatogramPlot.PreviewMouseWheel -= _chromatogramPlot_PreviewMouseWheel;
                    _chromatogramPlot.PreviewMouseDown -= _chromatogramPlot_PreviewMouseDown;
                    _hasMouseBeenDisabled = false;
                    return;
                }
            }
            else
            {
                _hasMouseBeenDisabled = true;
                _chromatogramPlot.PreviewMouseWheel += _chromatogramPlot_PreviewMouseWheel;
                _chromatogramPlot.PreviewMouseDown += _chromatogramPlot_PreviewMouseDown;
            }
        }

        private void _chromatogramPlot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void _chromatogramPlot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                return;
            }

            if (e.ClickCount == 1)
            {
                ChromPlot_MouseDown(sender, e);
                e.Handled = true;
                return;
            }
            e.Handled = true;
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

            _chromatogramPlot.Plot.Benchmark = new Polygon(new Coordinates[0]);

            _chromatogramViewModel = cvm;
            _spectrumViewModel = svm;

            // yay property changed events...
            PlotSettings.Instance.PropertyChanged += PlotSettings_PropertyChanged;
            PlotSettings.Instance.Chromatogram.PropertyChanged += PlotSettings_PropertyChanged;
            PlotSettings.Instance.Spectrum.PropertyChanged += PlotSettings_PropertyChanged;

            SetColormapByName("Ice", false);

            // subscribe to plot events
            _chromatogramPlot.MouseDown += ChromPlot_MouseDown;
            _chromatogramPlot.MouseDoubleClick += ChromPlot_MouseDoubleClick;

            //DisablePlotBenchmarking();
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
            if (_chromatogramViewModel.Times == null)
            {
                return;
            }
            var plt = _chromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("Intensity");

            var scatter = plt.Add.ScatterLine(x, y);
            scatter.LineWidth = 1.5F;
            scatter.Color = PlotSettings.Instance.Chromatogram.LineColor; 

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

        private void ResetLineColor()
        {
            var plots = _chromatogramPlot.Plot.GetPlottables();

            if (plots.Count() == 0)
            {
                return;
            }

            var line = plots.FirstOrDefault(plt => plt.ToString().Contains("Scatter")) as ScottPlot.Plottables.Scatter;
            line.Color = PlotSettings.Instance.Chromatogram.LineColor;
            _chromatogramPlot.Refresh();
        }

        public void Plot2DChromatogram(string style)
        {
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

            double lineLoc = (PlotSettings.Instance.Chromatogram.Style == "Line") ? _chromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1] : PlotSettings.Instance.ScanNumber;

            var line = plt.Add.VerticalLine(lineLoc);
            line.LineWidth = 1;
            line.Color = ScottPlot.Color.FromHex("#0f0f0f");
            line.IsVisible = PlotSettings.Instance.Chromatogram.VLineEnabled;
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

            var idx = PlotSettings.Instance.ScanNumber;
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

        private void ResetScalingX(bool auto)
        {
            if (auto)
            {
                _chromatogramPlot.Plot.Axes.AutoScaleX();
                _chromatogramPlot.Refresh();
                return;
            }
            // auto was false, so scale manually.
            SetManualLimits("X");
        }
        private void ResetScalingY(bool auto)
        {
            if (auto)
            {
                _chromatogramPlot.Plot.Axes.AutoScaleY();
                _chromatogramPlot.Refresh();
                return;
            }
            // auto was false, so scale manually.
            SetManualLimits("Y");
        }

        private void SetManualLimits(string axis)
        {
            if (axis == "X")
            {
                _chromatogramPlot.Plot.Axes.SetLimitsX(PlotSettings.Instance.Chromatogram.XMin, PlotSettings.Instance.Chromatogram.XMax);
                _chromatogramPlot.Refresh();
            }

            if (axis == "Y")
            {
                _chromatogramPlot.Plot.Axes.SetLimitsY(PlotSettings.Instance.Chromatogram.YMin,
                    PlotSettings.Instance.Chromatogram.YMax);
                _chromatogramPlot.Refresh();
            }
        }

        private void ChromPlot_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var limits = _chromatogramPlot.Plot.Axes.GetLimits();

            PlotSettings.Instance.Chromatogram.XMin = limits.XRange.Min;
            PlotSettings.Instance.Chromatogram.XMax = limits.XRange.Max;
            PlotSettings.Instance.Chromatogram.YMin = limits.YRange.Min;
            PlotSettings.Instance.Chromatogram.YMax = limits.YRange.Max;

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

                if (PlotSettings.Instance.Chromatogram.Style == "Line")
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

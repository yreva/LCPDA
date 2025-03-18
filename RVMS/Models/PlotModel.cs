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
using System.Windows.Media;
using System.Xml.Linq;
using Microsoft.VisualBasic;
using RawVision.ViewModels;
using RawVision.Views;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.Panels;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using Range = ScottPlot.Range;

namespace RawVision.Models
{
    /*------------------------------------------------------------------------------------------------------------
     *                                                 PlotModel                                                 *
     * Manages how data is visualized for both the chromatogram and spectrum plots. Updates are often triggered  *
     * by property changes in the PlotSettings singleton class. It is recommended to avoid storing data in here  *
     * as tempting as that is.                                                                                   *
     ----------------------------------------------------------------------------------------------------------- */
    public class PlotModel : INotifyPropertyChanged
    {
        //                          Implement Property Changed
        //        TODO: can probably remove as not many things are saved here
        /******************************************************************************/
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        //                     Event for PlotSettings PropertyChanged
        //           Most things involving updating the plots fire off from here
        /******************************************************************************/
        private void PlotSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ScanNumber":
                    CheckValidScanNumber();
                    PlotMassSpectrum();
                    ResetVlineOnChomatogram();
                    break;

                case "MassRangeLimitEnabled":
                    _spectrumViewModel.TrimDataToMassRange();
                    TrimmedDataChanged();
                    break;

            }
        }

        private void PlotSettings_ChromatogramPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
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
                case "ColorMin":
                case "ColorMax":
                    SetManualLimits("Color");
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

        private void PlotSettings_SpectrumPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AutoScaleX":
                    ResetSpectrumScalingX(true);
                    _spectrumPlot.Refresh();
                    break;

                case "AutoScaleY":
                    ResetSpectrumScalingY(true);
                    _spectrumPlot.Refresh();
                    break;

                case "XMin":
                case "XMax":
                    SetSpectrumManualLimits("X");
                    break;

                case "YMin":
                case "YMax":
                    SetSpectrumManualLimits("Y");
                    break;

                case "ColorMin":
                case "ColorMax":
                    SetSpectrumManualLimits("Color");
                    break;

                case "BarColor":
                    ResetBarColor();
                    break;

                case "GridEnabled":
                    SpectrumGridSettingChanged();
                    break;

                case "MouseEventsEnabled":
                    SpectrumMouseEventSettingChanged();
                    break;

                case "HoldManualLimits":
                    SetSpectrumManualLimits("X");
                    SetSpectrumManualLimits("Y");
                    break;

            }
        }

        //                              Class variables
        /******************************************************************************/
        private WpfPlot _chromatogramPlot;                    // Chromatogram Plot
        private WpfPlot _spectrumPlot;                        // Spectrum Plot
        private ScottPlot.Panels.ColorBar _colorbar;          // Colorbar (cannot be retrieved from plot, so have to keep reference)
        private ChromatogramViewModel _chromatogramViewModel; // Chromatogram data
        private SpectrumViewModel _spectrumViewModel;         // Spectrum data

        private IColormap _colormap;
        public IColormap Colormap
        {
            get { return _colormap; }
            set { _colormap = value; }
        }

        //                              Main Constructor
        /******************************************************************************/
        public PlotModel(WpfPlot cp, WpfPlot sp, ChromatogramViewModel cvm, SpectrumViewModel svm)
        {
            _chromatogramPlot = cp;
            _spectrumPlot = sp;

            _chromatogramPlot.Plot.Benchmark = new Polygon(new Coordinates[0]);
            _spectrumPlot.Plot.Benchmark = new Polygon(new Coordinates[0]);

            _chromatogramViewModel = cvm;
            _spectrumViewModel = svm;

            // yay property changed events...
            PlotSettings.Instance.PropertyChanged += PlotSettings_PropertyChanged;
            PlotSettings.Instance.Chromatogram.PropertyChanged += PlotSettings_ChromatogramPropertyChanged;
            PlotSettings.Instance.Spectrum.PropertyChanged += PlotSettings_SpectrumPropertyChanged;

            SetColormapByName("Ice", false);

            // subscribe to plot events
            _chromatogramPlot.MouseDown += ChromPlot_MouseDown;
            _chromatogramPlot.MouseDoubleClick += ChromPlot_MouseDoubleClick;
            _spectrumPlot.MouseDoubleClick += SpectrumPlot_MouseDoubleClick;

            //DisablePlotBenchmarking();
        }


        //           Helper methods for properties changed in double-click window
        /******************************************************************************/
        private void VLineSettingChanged()
        {
            var plt = _chromatogramPlot.Plot;

            if (plt.GetPlottables().Count() == 0)
            {
                return;
            }

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

        private void SpectrumGridSettingChanged()
        {
            if (PlotSettings.Instance.Spectrum.GridEnabled)
            {
                _spectrumPlot.Plot.ShowGrid();
            }
            else
            {
                _spectrumPlot.Plot.HideGrid();
            }
            _spectrumPlot.Refresh();
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

        private bool _hasMouseBeenDisabledForSpectrum = false;
        private void SpectrumMouseEventSettingChanged()
        {
            if (PlotSettings.Instance.Spectrum.MouseEventsEnabled)
            {
                if (_hasMouseBeenDisabled)
                {
                    _spectrumPlot.PreviewMouseWheel -= _chromatogramPlot_PreviewMouseWheel;
                    _spectrumPlot.PreviewMouseDown -= _chromatogramPlot_PreviewMouseDown;
                    _hasMouseBeenDisabledForSpectrum = false;
                    return;
                }
            }
            else
            {
                _hasMouseBeenDisabledForSpectrum = true;
                _spectrumPlot.PreviewMouseWheel += _chromatogramPlot_PreviewMouseWheel;
                _spectrumPlot.PreviewMouseDown += _chromatogramPlot_PreviewMouseDown;
            }
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

        private void ResetBarColor()
        {
            var plots = _spectrumPlot.Plot.GetPlottables();

            if (plots.Count() == 0)
            {
                return;
            }

            var line = plots.FirstOrDefault(plt => plt.ToString().Contains("Bar")) as ScottPlot.Plottables.BarPlot;
            line.Color = PlotSettings.Instance.Spectrum.BarColor;
            _spectrumPlot.Refresh();
        }

        //                           Basic plotting methods
        /******************************************************************************/
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
            vline.IsVisible = PlotSettings.Instance.Chromatogram.VLineEnabled;

            plt.Axes.AutoScale();

            if (_colorbar != null)
            {
                _colorbar.IsVisible = false;
            }

            _chromatogramPlot.Refresh();
        }
        public void Plot2DChromatogram()
        {
            if (PlotSettings.Instance.Chromatogram.MapScaling == "Linear")
            {
                PlotLinearMzMap();
            }

            else if (PlotSettings.Instance.Chromatogram.MapScaling == "Log10")
            {
                PlotLog10MzMap();
            }
        }
        private void PlotLinearMzMap()
        {
            double[] x;
            double[] y;
            double[,] z;

            if (PlotSettings.Instance.MassRangeLimitEnabled)
            {
                x = _chromatogramViewModel.Times;
                y = _spectrumViewModel.SlicedUniqueMasses.ToArray();
                z = _spectrumViewModel.SlicedIntensities2D;
            }
            else
            {
                x = _chromatogramViewModel.Times;
                y = _spectrumViewModel.UniqueMasses.ToArray();
                z = _spectrumViewModel.Intensities2D;
            }

            var plt = _chromatogramPlot.Plot;
            plt.Clear();
            
            var hm = plt.Add.Heatmap(z);

            hm.Colormap = Colormap;
            hm.Smooth = false;
            hm.FlipRows = true;
            hm.Extent = new CoordinateRect(x.Min(),x.Max(),y.Min(),y.Max());

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
            vline.IsVisible = PlotSettings.Instance.Chromatogram.VLineEnabled;


            plt.XLabel("Retention Time / min");
            plt.YLabel("m/z");
            plt.Axes.AutoScale();

            _chromatogramPlot.Refresh();
        }
        private void PlotLog10MzMap()
        {
            double[] x;
            double[] y;
            double[,] z;

            if (PlotSettings.Instance.MassRangeLimitEnabled)
            {
                x = _chromatogramViewModel.Times;
                y = _spectrumViewModel.SlicedUniqueMasses.ToArray(); // TODO: swap with XIC?
                z = _spectrumViewModel.SlicedLog10Intensities2D;
            }
            else
            {
                x = _chromatogramViewModel.Times;
                y = _spectrumViewModel.UniqueMasses.ToArray();
                z = _spectrumViewModel.Log10Intensities2D;
            }

            var plt = _chromatogramPlot.Plot;
            plt.Clear();

            var hm = plt.Add.Heatmap(z);

            hm.Colormap = Colormap;
            hm.Smooth = false;
            hm.FlipRows = true;
            hm.Extent = new CoordinateRect(x.Min(), x.Max(), y.Min(), y.Max());

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
            vline.IsVisible = PlotSettings.Instance.Chromatogram.VLineEnabled;


            plt.XLabel("Retention Time / min");
            plt.YLabel("m/z");
            plt.Axes.AutoScale();


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

            double[] x;
            double[] y;

            if (PlotSettings.Instance.MassRangeLimitEnabled)
            {
                (x, y) = TrimDataToMassRange(_spectrumViewModel.MZ[PlotSettings.Instance.ScanNumber - 1], _spectrumViewModel.Intensity[PlotSettings.Instance.ScanNumber - 1]);

            }
            else
            {
                x = _spectrumViewModel.MZ[PlotSettings.Instance.ScanNumber - 1];
                y = _spectrumViewModel.Intensity[PlotSettings.Instance.ScanNumber - 1];
            }

            var bars = plt.Add.Bars(x, y);
            bars.Color = PlotSettings.Instance.Spectrum.BarColor;

            plt.XLabel("m/z");
            plt.YLabel("Intensity");

            if (PlotSettings.Instance.Spectrum.HoldManualLimits)
            {
                plt.Axes.SetLimits(PlotSettings.Instance.Spectrum.XMin,
                    PlotSettings.Instance.Spectrum.XMax,
                    PlotSettings.Instance.Spectrum.YMin,
                    PlotSettings.Instance.Spectrum.YMax);
            }
            else
            {
                plt.Axes.AutoScale();
            }
            
            plt.Axes.AntiAlias(true);

            _spectrumPlot.Refresh();
        }


        //                Plotting methods for plotting sliced data
        //       TODO: modify regular plotting methods to handle both of these.
        /******************************************************************************/

        private void TrimmedDataChanged()
        {
            PlotMassSpectrum();

            if (PlotSettings.Instance.Chromatogram.Style == "Map")
            {
                Plot2DChromatogram();
            }
            else
            {
                PlotChromatogram();
                // can potentially add logic for plotting XIC/SIC here.
            }

        }

        //                Utility functions for data plotting
        /******************************************************************************/
        private void ResetVlineOnChomatogram()
        {
            var plt = _chromatogramPlot.Plot;
            var vline = plt.PlottableList.FirstOrDefault(x => x.ToString().Contains("VerticalLine"));
            plt.PlottableList.Remove(vline);

            double lineLoc = _chromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1];

            var line = plt.Add.VerticalLine(lineLoc);
            line.LineWidth = 1;
            line.Color = ScottPlot.Color.FromHex("#0f0f0f");
            line.IsVisible = PlotSettings.Instance.Chromatogram.VLineEnabled;
            _chromatogramPlot.Refresh();
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
                var limits = _chromatogramPlot.Plot.Axes.GetLimits();
                PlotSettings.Instance.Chromatogram.XMin = limits.Left;
                PlotSettings.Instance.Chromatogram.XMax = limits.Right;
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
                var limits = _chromatogramPlot.Plot.Axes.GetLimits();
                PlotSettings.Instance.Chromatogram.YMin = limits.Bottom;
                PlotSettings.Instance.Chromatogram.YMax = limits.Top;
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
                return;
            }

            if (axis == "Y")
            {
                _chromatogramPlot.Plot.Axes.SetLimitsY(PlotSettings.Instance.Chromatogram.YMin,
                    PlotSettings.Instance.Chromatogram.YMax);
                _chromatogramPlot.Refresh();
                return;
            }

            if (axis == "Color")
            {
                if (double.IsNaN(PlotSettings.Instance.Chromatogram.ColorMin) ||
                    double.IsNaN(PlotSettings.Instance.Chromatogram.ColorMax))
                {
                    return;
                }

                if (PlotSettings.Instance.Chromatogram.ColorMin > PlotSettings.Instance.Chromatogram.ColorMax)
                {
                    return;
                }

                var hm =
                    _chromatogramPlot.Plot.GetPlottables()
                        .FirstOrDefault(hm => hm.ToString().Contains("Heatmap"))
                        as Heatmap;
                hm.ManualRange = new Range(PlotSettings.Instance.Chromatogram.ColorMin,
                    PlotSettings.Instance.Chromatogram.ColorMax);
                _chromatogramPlot.Refresh();
            }
        }

        private void SetSpectrumManualLimits(string axis)
        {
            if (axis == "X")
            {
                _spectrumPlot.Plot.Axes.SetLimitsX(PlotSettings.Instance.Spectrum.XMin, PlotSettings.Instance.Spectrum.XMax);
                _spectrumPlot.Refresh();
                return;
            }

            if (axis == "Y")
            {
                _spectrumPlot.Plot.Axes.SetLimitsY(PlotSettings.Instance.Spectrum.YMin,
                    PlotSettings.Instance.Spectrum.YMax);
                _spectrumPlot.Refresh();
                return;
            }

        }

        private void ResetSpectrumScalingX(bool auto)
        {
            if (PlotSettings.Instance.Spectrum.HoldManualLimits == true)
            {
                return;
            }

            if (auto)
            {
                _spectrumPlot.Plot.Axes.AutoScaleX();
                var limits = _spectrumPlot.Plot.Axes.GetLimits();
                PlotSettings.Instance.Spectrum.XMin = limits.Left;
                PlotSettings.Instance.Spectrum.XMax = limits.Right;
                return;
            }
            // auto was false, so scale manually.
            SetSpectrumManualLimits("X");
        }
        private void ResetSpectrumScalingY(bool auto)
        {
            if (auto)
            {
                _spectrumPlot.Plot.Axes.AutoScaleY();
                var limits = _spectrumPlot.Plot.Axes.GetLimits();
                PlotSettings.Instance.Spectrum.YMin = limits.Bottom;
                PlotSettings.Instance.Spectrum.YMax = limits.Top;
                return;
            }
            // auto was false, so scale manually.
            SetSpectrumManualLimits("Y");
        }

        public (double[], double[]) TrimDataToMassRange(double[] xData, double[] yData)
        {
            var min = PlotSettings.Instance.MassRangeMinimum;
            var max = PlotSettings.Instance.MassRangeMaximum;

            // Get the indices of values between Min and Max
            var indices = xData
                .Select((value, index) => new { value, index })
                .Where(pair => pair.value >= min && pair.value <= max)
                .Select(pair => pair.index)
                .ToArray();


            var sliced_xData = indices.Select(i => xData[i]).ToArray();
            var sliced_yData = indices.Select(i => yData[i]).ToArray();

            return (sliced_xData, sliced_yData);
        }

        //                Helper methods for mouse/plot interactions
        /******************************************************************************/
        private void _chromatogramPlot_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void _chromatogramPlot_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && (sender as WpfPlot).Name == "ChromatogramPlot")
            {
                // allow for double clicks to open options again if closed
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

                view.Top = Application.Current.MainWindow.Top;
                view.Left = Application.Current.MainWindow.Left + Application.Current.MainWindow.Width;

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

        private void SpectrumPlot_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var limits = _spectrumPlot.Plot.Axes.GetLimits();

            PlotSettings.Instance.Spectrum.XMin = limits.XRange.Min;
            PlotSettings.Instance.Spectrum.XMax = limits.XRange.Max;
            PlotSettings.Instance.Spectrum.YMin = limits.YRange.Min;
            PlotSettings.Instance.Spectrum.YMax = limits.YRange.Max;

            var window = Application.Current.Windows.OfType<SpectrumOptionsView>().FirstOrDefault();
            if (window == null)
            {
                SpectrumOptionsView view = new SpectrumOptionsView();

                view.Top = Application.Current.MainWindow.Top + view.Height;
                view.Left = Application.Current.MainWindow.Left + Application.Current.MainWindow.Width;

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

                // Get the DPI scale factor
                PresentationSource source = PresentationSource.FromVisual((Visual)sender);
                double dpiX = 1.0, dpiY = 1.0;
                if (source?.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }

                // Adjust coordinates
                x *= dpiX;
                y *= dpiY;

                double clickedX = _chromatogramPlot.Plot.GetCoordinates(new Pixel(x, y)).X;

                // Find the closest time point
                double nearestTime = _chromatogramViewModel.Times.OrderBy(x => Math.Abs(x - clickedX)).FirstOrDefault();
                // Update ViewModel
                PlotSettings.Instance.ScanNumber = GetScanNumberFromRetentionTime(nearestTime);
            }
        }

        private void CheckValidScanNumber()
        {
            int value = PlotSettings.Instance.ScanNumber;

            if (value < 1)
            {
                PlotSettings.Instance.ScanNumber = 1;
            }

            if (value > _chromatogramViewModel.Times.Length)
            {
                PlotSettings.Instance.ScanNumber = _chromatogramViewModel.Times.Length;
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

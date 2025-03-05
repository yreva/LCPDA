using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using LCPDA.ViewModels;
using MyWpfApp.ViewModels;
using ScottPlot;
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

            switch (propertyName)
            {
                case "ScanNumber":
                    PlotMassSpectrum();
                    ResetVlineOnChomatogram();
                    break;

            }
        }

        // vars for plots from UI
        private WpfPlot _chromatogramPlot;
        private WpfPlot _spectrumPlot;

        private ChromatogramViewModel _chromatogramViewModel;
        private SpectrumViewModel _spectrumViewModel;

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

        private int _scanNumber;
        public int ScanNumber
        {
            get { return _scanNumber; }
            set
            {
                _scanNumber = value;
                OnPropertyChanged(nameof(ScanNumber));
            }
        }

        public PlotModel(WpfPlot cp, WpfPlot sp, ChromatogramViewModel cvm, SpectrumViewModel svm)
        {
            _chromatogramPlot = cp;
            _spectrumPlot = sp;
            _chromatogramViewModel = cvm;
            _spectrumViewModel = svm;

            _chromatogramPlot.MouseDown += ChromPlot_MouseDown;
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

            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[ScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

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

            hm.Colormap = new ScottPlot.Colormaps.Magma().Reversed();
            hm.Smooth = true;

            var cb = plt.Add.ColorBar(hm);
            cb.Label = "Intensity";
            //cb.LabelStyle.FontSize = 12;
            //hm.Axes.XAxis.Min = _chromatogramViewModel.Times.Min();
            //hm.Axes.XAxis.Max = _chromatogramViewModel.Times.Max();
            //hm.Axes.YAxis.Min = _spectrumViewModel.UniqueMasses.Min();
            //hm.Axes.YAxis.Max = _spectrumViewModel.UniqueMasses.Max();


            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[ScanNumber - 1]);
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

            hm.Colormap = new ScottPlot.Colormaps.Magma().Reversed();
            hm.Smooth = true;

            var cb = plt.Add.ColorBar(hm);
            cb.Label = "Log(Intensity)";
            //cb.LabelStyle.FontSize = 12;
            //hm.Axes.XAxis.Min = _chromatogramViewModel.Times.Min();
            //hm.Axes.XAxis.Max = _chromatogramViewModel.Times.Max();
            //hm.Axes.YAxis.Min = _spectrumViewModel.UniqueMasses.Min();
            //hm.Axes.YAxis.Max = _spectrumViewModel.UniqueMasses.Max();


            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[ScanNumber - 1]);
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

            double lineLoc = (_chromatogramStyle == "Line") ? _chromatogramViewModel.Times[ScanNumber - 1] : ScanNumber;

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

            var x = _spectrumViewModel.MZ[ScanNumber - 1];
            var y = _spectrumViewModel.Intensity[ScanNumber - 1];

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

                if (ChromatogramStyle == "Line")
                {
                    // Find the closest time point
                    double nearestTime = _chromatogramViewModel.Times.OrderBy(x => Math.Abs(x - clickedX)).FirstOrDefault();
                    // Update ViewModel
                    ScanNumber = GetScanNumberFromRetentionTime(nearestTime);
                }
                else
                {
                    ScanNumber = (int)Math.Abs(clickedX);
                }

            }
        }
    }
}

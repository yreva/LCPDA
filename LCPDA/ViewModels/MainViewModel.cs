using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System;
using Microsoft.Win32;
using MyWpfApp.ViewModels;
using LCPDA.Models;
using ThermoFisher.CommonCore.Data.Business;
using ScottPlot.WPF;
using System.Windows.Controls;
using ScottPlot;
using System.Windows.Shapes;
using System.Drawing;
using ScottPlot.Colormaps;
using SkiaSharp;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
using System.Reflection;
using System.IO;

namespace LCPDA.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Implement PropertyChanged methods
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            switch (propertyName)
            {
                case "CurrentScanNumber":
                    PlotMassSpectrum();
                    ResetVlineOnChomatogram();
                    break;

            }
        }

        // declare private View Models
        private ChromatogramViewModel _chromatogramViewModel;
        private SpectrumViewModel _spectrumViewModel;
        private IOModel _ioModel;

        // expose View Models to public
        public ChromatogramViewModel ChromatogramViewModel
        {
            get => _chromatogramViewModel;
            set
            {
                _chromatogramViewModel = value;
                OnPropertyChanged(nameof(ChromatogramViewModel));
            }
        }
        public SpectrumViewModel SpectrumViewModel
        {
            get => _spectrumViewModel;
            set
            {
                _spectrumViewModel = value;
                OnPropertyChanged(nameof(SpectrumViewModel));
            }
        }

        // define plot elements for UI
        private WpfPlot _chromatogramPlot = new WpfPlot();
        public WpfPlot ChromatogramPlot
        {
            get => _chromatogramPlot;
        }
        private WpfPlot _spectrumPlot = new WpfPlot();
        public WpfPlot SpectrumPlot
        {
            get => _spectrumPlot;
        }

        // Main Class Constructor
        public MainViewModel()
        {
            OpenFileCommand = new RelayCommand(OpenFile);
            LoadFileCommand = new RelayCommand(LoadFilePressed);
            SaveDataCommand = new RelayCommand(SaveDataPressed);

            // Initialize the ViewModels for both plots
            ChromatogramViewModel = new ChromatogramViewModel();
            SpectrumViewModel = new SpectrumViewModel();
            _ioModel = new IOModel();

            // subscribe to events
            ChromatogramPlot.MouseDown += ChromPlot_MouseDown;
        }

        private string _currentChromatogramStyle;

        private int _currentScanNumber = 1;
        public int CurrentScanNumber
        {
            get
            {
                return _currentScanNumber;
            }
            set
            {
                if (_currentScanNumber == value)
                {
                    return;
                }

                if (value < 1)
                {
                    value = 1;
                }

                if (value > ChromatogramViewModel.Times.Length)
                {
                    value = ChromatogramViewModel.Times.Length;
                }

                _currentScanNumber = (int)value;
                OnPropertyChanged(nameof(CurrentScanNumber));
            }
        }

        public void IncrementScan(int increment)
        {
            if (ChromatogramViewModel.Times == null)
            {
                return;
            }
            CurrentScanNumber += increment;
            return;
        }

        private int _massResolutionDecimal;
        public int MassResolutionDecimal
        {
            get => _massResolutionDecimal;
            set
            {
                if (value != _massResolutionDecimal)
                {
                    _massResolutionDecimal = value;
                    _spectrumViewModel.SetMassResolution(value);
                    OnPropertyChanged(nameof(MassResolutionDecimal));
                }
            }
        }

        private string _mapScalingMethod = "Linear";
        public string MapScalingMethod
        {
            get => _mapScalingMethod;
            set
            {
                if (value != _mapScalingMethod)
                {
                    _mapScalingMethod = value;
                    OnPropertyChanged(nameof(MapScalingMethod));
                }
            }
        }

        public void MassResolutionChanged()
        {
            // Initialize the ViewModels for both plots
            ChromatogramViewModel = new ChromatogramViewModel();
            SpectrumViewModel = new SpectrumViewModel();
            LoadFilePressed();
        }

        public ICommand LoadFileCommand { get; }
        public async void LoadFilePressed()
        {
            //HandleLoadingPopup("Start");

            if (SelectedFilePath == null)
            {
                return;
            }

            _currentScanNumber = 1;
            _ioModel.OpenRawFile(SelectedFilePath);
            var rf = _ioModel.GetRawFileFromIOModel();
            _chromatogramViewModel.SetRawFile(rf);
            _spectrumViewModel.SetMassResolution(MassResolutionDecimal);

            _spectrumViewModel.SetRawFile(rf);


            switch (SelectedOption)
            {
                case "Line":
                    _currentChromatogramStyle = "Line";
                    PlotChromatogram();
                    break;
                case "Map":
                    _currentChromatogramStyle = "Map";
                    Plot2DChromatogram();
                    break;
            }

            PlotMassSpectrum();

            //HandleLoadingPopup("Stop");
        }

        private void HandleLoadingPopup(string process)
        {
            if (process == "Start")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProgressWindow progressWindow = new ProgressWindow();
                    progressWindow.Show();
                });
            }
            else if (process == "Stop")
            {
                var window = Application.Current.Windows.OfType<ProgressWindow>().FirstOrDefault();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    //window.CloseWindow();
                });
            }
        }

        public ICommand OpenFileCommand { get; }
        public void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a File",
                Filter = "Raw Files|*.raw;*.Raw;*.RAW",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                SelectedFilePath = filePath;
                //MessageBox.Show($"Selected file: {filePath}");
                LoadFilePressed();
            }
        }

        private string _selectedFilePath;
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                _selectedFilePath = value;
                OnPropertyChanged(nameof(SelectedFilePath));
            }
        }

        private void PlotChromatogram()
        {
            var plt = ChromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("Intensity");

            var scatter = plt.Add.ScatterLine(x, y);
            scatter.LineWidth = 1.5F;

            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[CurrentScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

            ChromatogramPlot.Refresh();
        }

        private void Plot2DChromatogram()
        {
            if (SpectrumViewModel.Intensities2D == null)
            {
                return;
            }

            if (MapScalingMethod == "Linear")
            {
                PlotLinearMzMap();
            }

            else if (MapScalingMethod == "Log10")
            {
                PlotLog10MzMap();
            }
        }

        private void PlotLinearMzMap()
        {
            var plt = ChromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("m/z");

            double[,] flippedData = FlipVertically(SpectrumViewModel.Intensities2D);

            var hm = plt.Add.Heatmap(SpectrumViewModel.Intensities2D);

            hm.Colormap = new Magma().Reversed();
            hm.Smooth = true;

            var cb = plt.Add.ColorBar(hm);
            cb.Label = "Intensity";
            //cb.LabelStyle.FontSize = 12;
            //hm.Axes.XAxis.Min = _chromatogramViewModel.Times.Min();
            //hm.Axes.XAxis.Max = _chromatogramViewModel.Times.Max();
            //hm.Axes.YAxis.Min = _spectrumViewModel.UniqueMasses.Min();
            //hm.Axes.YAxis.Max = _spectrumViewModel.UniqueMasses.Max();


            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[CurrentScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

            ChromatogramPlot.Refresh();
        }

        private void PlotLog10MzMap()
        {
            var plt = ChromatogramPlot.Plot;
            plt.Clear();

            var x = _chromatogramViewModel.Times;
            var y = _chromatogramViewModel.TIC;

            plt.XLabel("Retention Time / min");
            plt.YLabel("m/z");

            double[,] flippedData = FlipVertically(SpectrumViewModel.Log10Intensities2D);

            var hm = plt.Add.Heatmap(flippedData);

            hm.Colormap = new Magma().Reversed();
            hm.Smooth = true;

            var cb = plt.Add.ColorBar(hm);
            cb.Label = "Log(Intensity)";
            //cb.LabelStyle.FontSize = 12;
            //hm.Axes.XAxis.Min = _chromatogramViewModel.Times.Min();
            //hm.Axes.XAxis.Max = _chromatogramViewModel.Times.Max();
            //hm.Axes.YAxis.Min = _spectrumViewModel.UniqueMasses.Min();
            //hm.Axes.YAxis.Max = _spectrumViewModel.UniqueMasses.Max();


            var vline = plt.Add.VerticalLine(_chromatogramViewModel.Times[CurrentScanNumber - 1]);
            vline.LineWidth = 1;
            vline.Color = ScottPlot.Color.FromHex("#0f0f0f");

            plt.Axes.AutoScale();

            ChromatogramPlot.Refresh();
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
            var plt = ChromatogramPlot.Plot;
            var vline = plt.PlottableList.FirstOrDefault(x => x.ToString().Contains("VerticalLine"));
            plt.PlottableList.Remove(vline);

            double lineLoc = (_currentChromatogramStyle == "Line") ? _chromatogramViewModel.Times[CurrentScanNumber - 1] : CurrentScanNumber;

            var line = plt.Add.VerticalLine(lineLoc);
            line.LineWidth = 1;
            line.Color = ScottPlot.Color.FromHex("#0f0f0f");
            ChromatogramPlot.Refresh();
        }

        private void PlotMassSpectrum()
        {
            var plt = SpectrumPlot.Plot;

            plt.Clear();

            var x = _spectrumViewModel.MZ[CurrentScanNumber - 1];
            var y = _spectrumViewModel.Intensity[CurrentScanNumber - 1];

            plt.Add.Bars(x, y);

            plt.XLabel("m/z");
            plt.YLabel("Intensity");

            plt.Axes.AutoScale();
            plt.Axes.AntiAlias(true);

            SpectrumPlot.Refresh();
        }

        private void ChromatogramStyleChanged()
        {
            switch (SelectedOption)
            {
                case "Line":
                    _currentChromatogramStyle = "Line";
                    PlotChromatogram();
                    break;
                case "Map":
                    _currentChromatogramStyle = "Map";
                    Plot2DChromatogram();
                    break;
            }
        }

        public void ScalingMethodChanged()
        {
            //
            if (_currentChromatogramStyle == "Line")
            {
                return;
            }

            Plot2DChromatogram();
        }

        public int GetScanNumberFromRetentionTime(double rt)
        {
            int x = Array.FindIndex(_chromatogramViewModel.Times, t => t == rt);
            return x + 1;
        }

        private void ChromPlot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ChromatogramViewModel.Times == null)
            {
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var mouse = e.GetPosition(ChromatogramPlot);
                var x = mouse.X;
                var y = mouse.Y;
                double clickedX = ChromatogramPlot.Plot.GetCoordinates(new Pixel(x, y)).X;

                if (_currentChromatogramStyle == "Line")
                {
                    // Find the closest time point
                    double nearestTime = ChromatogramViewModel.Times.OrderBy(x => Math.Abs(x - clickedX)).FirstOrDefault();
                    // Update ViewModel
                    CurrentScanNumber = GetScanNumberFromRetentionTime(nearestTime);
                }
                else
                {
                    CurrentScanNumber = (int)Math.Abs(clickedX);
                }
                
            }
        }

        private string _selectedOption = "Line";
        public string SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                OnPropertyChanged(nameof(SelectedOption));
                ChromatogramStyleChanged();
            }
        }

        public ICommand SaveDataCommand { get; }
        public void SaveDataPressed()
        {
            _ioModel.WriteDataToCsv(_spectrumViewModel.UniqueMasses,_chromatogramViewModel.Times,_spectrumViewModel.Intensities2D);
        }
    } //end MainViewModel
}


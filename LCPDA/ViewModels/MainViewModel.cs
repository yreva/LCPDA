using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

using ScottPlot.WPF;
using ScottPlot;
using RawVision.Models;
using RawVision.Models;
using RawVision.Views;


namespace RawVision.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Implement PropertyChanged methods
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // declare private View Models
        private ChromatogramViewModel _chromatogramViewModel;
        private SpectrumViewModel _spectrumViewModel;
        private IOModel _ioModel;
        private PlotModel _plotModel;

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
            _plotModel = new PlotModel(_chromatogramPlot, _spectrumPlot, _chromatogramViewModel, _spectrumViewModel);
            _plotModel.ChromatogramStyle = _currentChromatogramStyle;
            _plotModel.PropertyChanged += PropertyChanged;
        }

        private string _currentChromatogramStyle = "Line";

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
            PlotSettings.Instance.ScanNumber += increment;
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
                    _plotModel.PlotChromatogram();
                    break;
                case "Map":
                    _currentChromatogramStyle = "Map";
                    Plot2DChromatogram();
                    break;
            }

            _plotModel.PlotMassSpectrum();

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

        private void Plot2DChromatogram()
        {
            if (_spectrumViewModel.Intensities2D == null)
            {
                return;
            }
            _plotModel.Plot2DChromatogram(MapScalingMethod);
        }

        private void ChromatogramStyleChanged()
        {
            switch (SelectedOption)
            {
                case "Line":
                    _currentChromatogramStyle = "Line";
                    _plotModel.PlotChromatogram();
                    break;
                case "Map":
                    _currentChromatogramStyle = "Map";
                    Plot2DChromatogram();
                    break;
            }
        }

        private double[,] RemoveLowIntensityRows(double[,] array, double multiplier)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            // Find the max value in the entire array
            double globalMax = array.Cast<double>().Max();
            double threshold = multiplier * globalMax;

            // Identify rows where max(row) >= threshold
            var validRows = Enumerable.Range(0, rows)
                .Where(r => Enumerable.Range(0, cols)
                    .Max(c => array[r, c]) >= threshold)
                .ToArray();

            // Create new array with only valid rows
            double[,] result = new double[validRows.Length, cols];

            for (int i = 0; i < validRows.Length; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = array[validRows[i], j];
                }
            }

            return result;
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

        public string GetColormapSetting()
        {
            return _plotModel.Colormap.Name;
        }

        public void SetColormapSetting(string name)
        {
            bool reversed = false;
            if (name.Contains("Reversed"))
            {
                reversed = true;
                name = name.Replace("Reversed","");
            }
            _plotModel.SetColormapByName(name,reversed);
        }

        public ICommand SaveDataCommand { get; }
        public void SaveDataPressed()
        {
            _ioModel.WriteDataToCsv(_spectrumViewModel.UniqueMasses,_chromatogramViewModel.Times,_spectrumViewModel.Intensities2D);
        }
    } //end MainViewModel
}


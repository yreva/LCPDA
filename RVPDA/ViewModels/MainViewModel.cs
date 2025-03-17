using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ScottPlot.WPF;
using ScottPlot;
using RVPDA.Models;
using RVPDA.Views;

namespace RVPDA.ViewModels
{
    /*------------------------------------------------------------------------------------------------------------
     *                                                MainViewModel                                              *
     * Serves the MainWindow and handles instances of the data managers (Chromatogram and Spectrum View Models)  *                                                                                         *
     ----------------------------------------------------------------------------------------------------------- */
    public class MainViewModel : INotifyPropertyChanged
    {
        //                        Implement PropertyChanged methods
        /******************************************************************************/
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //                          Declare private view models
        /******************************************************************************/
        private ChromatogramViewModel _chromatogramViewModel;
        private SpectrumViewModel _spectrumViewModel;
        private IOModel _ioModel;
        private PlotModel _plotModel;
        
        //                    Make public view models for the data managers
        /******************************************************************************/
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

        //                      Declare plots for the UI/MainWindow
        /******************************************************************************/
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

        //                       Constructor for MainViewModel
        /******************************************************************************/
        public MainViewModel()
        {
            OpenFileCommand = new RelayCommand(OpenFile);
            LoadFileCommand = new RelayCommand(LoadFilePressed);
            SaveDataCommand = new RelayCommand(SaveDataPressed);
            Command_ShowPeakList = new RelayCommand(ShowPeakListPressed);
            Command_ImportSpectrum = new RelayCommand(ImportSpectrumPressed);

            // Initialize the ViewModels for both plots
            ChromatogramViewModel = new ChromatogramViewModel();
            SpectrumViewModel = new SpectrumViewModel();
            _ioModel = new IOModel();
            _plotModel = new PlotModel(_chromatogramPlot,_spectrumPlot, _chromatogramViewModel, _spectrumViewModel);
            _plotModel.PropertyChanged += PropertyChanged;

            PlotSettings.Instance.PropertyChanged += PlotSettings_PropertyChanged;
            PlotSettings.Instance.Spectrum.ResetImportedSpectrum();
        }


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
            CurrentScanNumber = PlotSettings.Instance.ScanNumber;
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
                    //_spectrumViewModel.SetMassResolution(value);
                    OnPropertyChanged(nameof(MassResolutionDecimal));
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
            if (_spectrumViewModel.Intensity == null)
            {
                return;
            }
            _plotModel.Plot2DChromatogram();
        }

        private void ChromatogramStyleChanged()
        {
            switch (SelectedOption)
            {
                case "Line":
                    PlotSettings.Instance.Chromatogram.Style = "Line";
                    _plotModel.PlotChromatogram();
                    break;
                case "Map":
                    PlotSettings.Instance.Chromatogram.Style = "Map";
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
            if (PlotSettings.Instance.Chromatogram.Style == "Line")
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
                PlotSettings.Instance.Chromatogram.Style = value;
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


        //                      Commands for Buttons in MainWindow
        /******************************************************************************/

        public ICommand SaveDataCommand { get; }
        public void SaveDataPressed()
        {
            _ioModel.WriteDataToCsv(_spectrumViewModel.Wavelengths,_chromatogramViewModel.Times,_spectrumViewModel.Intensity);
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

        public ICommand LoadFileCommand { get; }
        public void LoadFilePressed()
        {
            //HandleLoadingPopup("Start");
            Mouse.OverrideCursor = Cursors.Wait;

            if (SelectedFilePath == null)
            {
                return;
            }

            _currentScanNumber = 1;
            _ioModel.OpenRawFile(SelectedFilePath);
            var rf = _ioModel.GetRawFileFromIOModel();
            _chromatogramViewModel.SetRawFile(rf);

            _spectrumViewModel.SetRawFile(rf);


            switch (SelectedOption)
            {
                case "Line":
                    PlotSettings.Instance.Chromatogram.Style = "Line";
                    _plotModel.PlotChromatogram();
                    break;
                case "Map":
                    PlotSettings.Instance.Chromatogram.Style = "Map";
                    Plot2DChromatogram();
                    break;
            }

            _plotModel.PlotSpectrum();

            Mouse.OverrideCursor = null;
            //HandleLoadingPopup("Stop");
        }

        public ICommand Command_ShowPeakList { get; }
        public void ShowPeakListPressed()
        {
            if (ChromatogramViewModel.Times == null)
            {
                MessageBox.Show("No data to display!","No Spectra",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            PeakListWindow window = new PeakListWindow();
            double rt = Math.Round(ChromatogramViewModel.Times[PlotSettings.Instance.ScanNumber - 1],2);
            window.Header.Text = string.Format("Scan #{0}, at {1} min", PlotSettings.Instance.ScanNumber - 1, rt);
            window.dataGrid.ItemsSource = SpectrumViewModel.CreatePeakList(PlotSettings.Instance.ScanNumber - 1);
            Mouse.OverrideCursor = null;
            window.Show();
        }

        public ICommand Command_ImportSpectrum { get; }
        public void ImportSpectrumPressed()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select a File",
                Filter = "CSV Files|*.csv;*.CSV",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                //MessageBox.Show($"Selected file: {filePath}");
                (double[] x, double[] y) = IOModel.LoadCsvColumns(filePath);
                _spectrumViewModel.AddImportedSpectrum(x,y);
                _plotModel.PlotImportedSpectrum(filePath);
                PlotSettings.Instance.Spectrum.SetImportedSpectrumPath(filePath);
            }

            var window = Application.Current.Windows.OfType<SpectrumOptionsView>().FirstOrDefault();
            if (window != null)
            {
                window.UpdateLayout();
            }
        }


        private void PlotSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ScanNumber":
                    CurrentScanNumber = PlotSettings.Instance.ScanNumber;
                    break;
            }
        }

    } //end MainViewModel
}


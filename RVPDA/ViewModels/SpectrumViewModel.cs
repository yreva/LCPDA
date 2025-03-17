using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Windows.Input;
using System.Windows.Media.Animation;
using OpenTK.Windowing.Common.Input;
using RVPDA.Views;


namespace RVPDA.ViewModels
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ObservableCollection<DataRow>> _peakLists;

        private double[] wavelengths;

        private double[,] intensity;
        private double[,] log10intensity;

        private List<double[]> intensityList;
        private List<double[]> log10intensityList;

        private double[] importedWavelength;
        private double[] importedAbsorbance;

        private List<int> scanNumbers;

        private IRawDataExtended _rawFile;

        public double[,] Intensity
        {
            get { return intensity; }
        }

        public double[,] Log10Intensity
        {
            get { return intensity; }
        }

        public List<double[]> IntensityList
        {
            get { return intensityList; }
        }

        public List<double[]> Log10IntensityList
        {
            get { return log10intensityList; }
        }

        public double[] Wavelengths
        {
            get { return wavelengths; }
        }

        public double[] ImportedWavelength
        {
            get { return importedWavelength; }
        }

        public double[] ImportedAbsorbance
        {
            get { return importedAbsorbance; }
        }

        private string _polarity;
        public string Polarity
        {
            get => _polarity;
            set
            {
                _polarity = value;
                OnPropertyChanged(nameof(Polarity));
            }
        }

        private string _numberOfScans;
        private int _numberOfScansInt;
        public string NumberOfScans
        {
            get => _numberOfScans;
            set
            {
                _numberOfScans = value;
                OnPropertyChanged(nameof(NumberOfScans));
            }
        }

        public ObservableCollection<ObservableCollection<DataRow>> PeakLists
        {
            get => _peakLists;
            set { _peakLists = value; }
        }

        public SpectralData Data { get; set; }

        public SpectrumViewModel()
        {
            Data = new SpectralData();
            wavelengths = new double[0];
            intensity = new double[0, 0];
            log10intensity = new double[0, 0];
            intensityList = new List<double[]>();
            log10intensityList = new List<double[]>();

            scanNumbers = new List<int>();

            PeakLists = new ObservableCollection<ObservableCollection<DataRow>>();
        }

        public void SetRawFile(IRawDataExtended rf)
        {
            _rawFile = rf;
            _numberOfScansInt = _rawFile.RunHeaderEx.SpectraCount;
            GetMassSpectra();
        }


        private void GetMassSpectra()
        {
            double[] times = new double[_numberOfScansInt];

            // Get the first and last scan from the RAW file
            int firstScanNumber = _rawFile.RunHeaderEx.FirstSpectrum;
            int lastScanNumber = _rawFile.RunHeaderEx.LastSpectrum;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            int i = 0;
            for (int sn = firstScanNumber; sn <= lastScanNumber; sn++)
            {
                var stats = _rawFile.GetScanStatsForScanNumber(sn);
                var scan = _rawFile.GetSegmentedScanFromScanNumber(sn);
                times[i] = _rawFile.RetentionTimeFromScanNumber(sn);

                if (sn == 1)
                {
                    wavelengths = scan.Positions;
                }

                intensity = new double[_numberOfScansInt, scan.Intensities.Length];
                log10intensity = new double[_numberOfScansInt, scan.Intensities.Length];

                var log10valuesForList = new double[scan.Intensities.Length];
                var valuesForList = new double[scan.Intensities.Length];

                for (int j = 0; j < scan.Intensities.Length; j++)
                {
                    intensity[i, j] = scan.Intensities[j]/1e6;
                    log10intensity[i,j] = scan.Intensities[j] <= 0 ? 0 : Math.Log10(scan.Intensities[j]);
                    log10valuesForList[j] = log10intensity[i, j];
                    valuesForList[j] = intensity[i, j];
                }

                scanNumbers.Add(sn);
                log10intensityList.Add(log10valuesForList);
                intensityList.Add(valuesForList);
                i++;
            }

            Data.SetAborbances(intensity);
            Data.SetTimes(times);
            Data.SetWavelengths(wavelengths);

            NumberOfScans = scanNumbers.Count().ToString();
            sw.Stop();
            Console.WriteLine("Time to get mass spectra: " + sw.ElapsedMilliseconds + " ms");

        }

        public void TrimDataToWavelengthRange()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var min = PlotSettings.Instance.WavelengthRangeMinimum;
            var max = PlotSettings.Instance.WavelengthRangeMaximum;

            var wl = Data.GetWavelengths();
            var au = Data.GetAbsorbances();
            var times = Data.GetTimes();

            // Get the indices of values between Min and Max
            var indices = wl
                .Select((value, index) => new { value, index })
                .Where(pair => pair.value >= min && pair.value <= max)
                .Select(pair => pair.index)
                .ToArray();

            // Create a new 2D array for the selected rows
            intensity = new double[au.GetLength(0), indices.Length];
            log10intensity = new double[au.GetLength(0), indices.Length];
            wavelengths = new double[indices.Length];

            // Fill the new array with the selected rows
            for (int i = 0; i < indices.Length; i++)
            {
                wavelengths[i] = wl[indices[i]];
                int columnIndex = indices[i];
                for (int j = 0; j < au.GetLength(0); j++)
                {
                    intensity[j, i] = au[j, columnIndex];
                    log10intensity[j, i] = au[j, columnIndex] <= 0 ? 0 : Math.Log10(au[j, columnIndex] *1e6);
                }
            }

            Mouse.OverrideCursor = null;
        }

        public ObservableCollection<DataRow> CreatePeakList(int ScanNumber)
        {
            ObservableCollection<DataRow> peaks = new ObservableCollection<DataRow>();

            for (int i = 0; i < intensityList[ScanNumber].Count(); i++)
            {
                DataRow row = new DataRow();
                row.Mass = wavelengths[i];
                row.Intensity = intensityList[ScanNumber][i];
                peaks.Add(row);
            }

            return peaks;
        }

        public void AddImportedSpectrum(double[] wavelengths, double[] absorbance)
        {
            importedWavelength = wavelengths;
            importedAbsorbance = absorbance;
        }   

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }

    public class DataRow
    {
        public double Mass { get; set; }
        public double Intensity { get; set; }
    }

    // this class is used to store the spectral data, without manipulation
    // and can be recalled to get the original data
    public class SpectralData
    {
        private double[] Wav;
        private double[] Time;
        private double[,] AU;

        // Constructor
        public SpectralData()
        {
            Wav = new double[0];
            Time = new double[0];
            AU = new double[0, 0];
        }

        public double[] GetWavelengths()
        {
            return Wav.Clone() as double[];
        }

        public void SetWavelengths(double[] wl)
        {
            Wav = wl.Clone() as double[];
            return;
        }

        public double[] GetTimes()
        {
            return Time.Clone() as double[];
        }

        public void SetTimes(double[] times)
        {
            Time = times.Clone() as double[];
            return;
        }

        public double[,] GetAbsorbances()
        {
            return AU.Clone() as double[,];
        }

        public void SetAborbances(double[,] abs)
        {
            AU = abs.Clone() as double[,];
            return;
        }
    }
}

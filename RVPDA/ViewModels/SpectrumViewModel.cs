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
using RVPDA.Views;


namespace RVPDA.ViewModels
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ObservableCollection<DataRow>> _peakLists;

        private List<double> uniqueMasses;

        private double[] wavelengths;

        private double[,] intensity;
        private double[,] log10intensity;

        private List<double[]> intensityList;
        private List<double[]> log10intensityList;

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

        public SpectrumViewModel()
        {
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
            List<int> massSpectraIdx = new List<int>();

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
                    valuesForList[j] = intensity[i, j]/1e6;
                }

                scanNumbers.Add(sn);
                log10intensityList.Add(log10valuesForList);
                intensityList.Add(valuesForList);
                i++;
            }

            Polarity = "";
            NumberOfScans = scanNumbers.Count().ToString();
            sw.Stop();
            Console.WriteLine("Time to get mass spectra: " + sw.ElapsedMilliseconds + " ms");

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
}

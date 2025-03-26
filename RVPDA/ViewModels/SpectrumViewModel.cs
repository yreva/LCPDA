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
using ThermoFisher.CommonCore.Data;


namespace RVPDA.ViewModels
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ObservableCollection<DataRow>> _peakLists;

        private double[] wavelengths;

        private double[,] intensityRaw;
        private double[,] intensitySliced;

        private double[,] log10intensityRaw;
        private double[,] log10intensitySliced;

        private List<double[]> intensityListSliced;
        private List<double[]> intensityListRaw;
        private List<double[]> log10intensityList;

        private double[] importedWavelength;
        private double[] importedAbsorbance;

        private List<int> scanNumbers;

        private IRawDataExtended _rawFile;

        public double[,] Intensity
        {
            get { return intensitySliced ?? intensityRaw; } 
        }

        public double[,] Log10Intensity
        {
            get { return log10intensitySliced ?? log10intensityRaw; }
        }

        public List<double[]> IntensityList
        {
            get { return intensityListSliced ?? intensityListRaw; }
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

        public SpectrumViewModel()
        {
            wavelengths = new double[0];
            intensityRaw = new double[0, 0];
            log10intensityRaw = new double[0, 0];
            intensitySliced = null;
            log10intensitySliced = null;
            intensityListRaw = new List<double[]>();
            intensityListSliced = null;
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

            double maxInt = 0;

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

                intensityRaw = new double[_numberOfScansInt, scan.Intensities.Length];
                log10intensityRaw = new double[_numberOfScansInt, scan.Intensities.Length];

                var log10valuesForList = new double[scan.Intensities.Length];
                var valuesForList = new double[scan.Intensities.Length];

                for (int j = 0; j < scan.Intensities.Length; j++)
                {
                    intensityRaw[i, j] = scan.Intensities[j];
                    log10intensityRaw[i,j] = scan.Intensities[j] <= 0 ? 0 : Math.Log10(scan.Intensities[j]);
                    log10valuesForList[j] = log10intensityRaw[i, j];
                    valuesForList[j] = intensityRaw[i, j];
                }
                
                scanNumbers.Add(sn);
                log10intensityList.Add(log10valuesForList);
                intensityListRaw.Add(scan.Intensities);
                i++;
            }

            NumberOfScans = scanNumbers.Count().ToString();
            sw.Stop();
            Console.WriteLine("Time to get PDA spectra: " + sw.ElapsedMilliseconds + " ms");

        }

        public void TrimDataToWavelengthRange()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var min = PlotSettings.Instance.WavelengthRangeMinimum;
            var max = PlotSettings.Instance.WavelengthRangeMaximum;

            var wl = wavelengths;
            var au = intensityListRaw;
            //var times = Data.GetTimes();

            // Get the indices of values between Min and Max
            var indices = wl
                .Select((value, index) => new { value, index })
                .Where(pair => pair.value >= min && pair.value <= max)
                .Select(pair => pair.index)
                .ToArray();

            // Create a new 2D array for the selected rows
            intensitySliced = new double[au.Count(), indices.Length];
            intensityListSliced = new List<double[]>();
            log10intensitySliced = new double[au.Count(), indices.Length];
            wavelengths = new double[indices.Length];

            // Fill the new array with the selected rows
            for (int i = 0; i < au.Count(); i++)
            {
                double[] valuesForList = new double[indices.Length];

                for (int j = 0; j < indices.Length; j++)
                {
                    int columnIndex = indices[j];

                    intensitySliced[i, j] = au[i][columnIndex];
                    log10intensitySliced[i, j] = au[i][columnIndex] <= 0 ? 0 : Math.Log10(au[i][columnIndex]);
                    valuesForList[j] = au[i][columnIndex];

                    if (i == 0)
                    {
                        wavelengths[j] = wl[columnIndex];
                    }
                }


                intensityListSliced.Add(valuesForList);

            }

            Mouse.OverrideCursor = null;
        }

        public ObservableCollection<DataRow> CreatePeakList(int ScanNumber)
        {
            ObservableCollection<DataRow> peaks = new ObservableCollection<DataRow>();

            for (int i = 0; i < IntensityList[ScanNumber].Count(); i++)
            {
                DataRow row = new DataRow();
                row.Mass = wavelengths[i];
                row.Intensity = IntensityList[ScanNumber][i];
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
}

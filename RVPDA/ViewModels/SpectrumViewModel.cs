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

        private double[] wavelengthsRaw;
        private double[] wavelengthsSliced;

        private List<double[]> intensityListSliced;
        private List<double[]> intensityListRaw;
        private List<double[]> log10intensityListSliced;
        private List<double[]> log10intensityListRaw;

        private double minIntensityRaw = double.MaxValue;
        private double maxIntensityRaw = double.MinValue;
        private double? minIntensitySliced = null;
        private double? maxIntensitySliced = null;

        private double[] importedWavelength;
        private double[] importedAbsorbance;

        private List<int> scanNumbers;

        private IRawDataExtended _rawFile;


        public List<double[]> IntensityList
        {
            get { return intensityListSliced ?? intensityListRaw; }
        }

        public List<double[]> Log10IntensityList
        {
            get { return log10intensityListSliced ?? log10intensityListRaw; }
        }

        public double MinIntensity
        {
            get { return minIntensitySliced ?? minIntensityRaw; }
        }

        public double MaxIntensity
        {
            get { return maxIntensitySliced ?? maxIntensityRaw; }
        }


        public double[] Wavelengths
        {
            get { return wavelengthsSliced ?? wavelengthsRaw; }
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
            wavelengthsRaw = new double[0];
            intensityListRaw = new List<double[]>();
            log10intensityListRaw = new List<double[]>();

            wavelengthsSliced = null;
            intensityListSliced = null;
            log10intensityListSliced = null;

            scanNumbers = new List<int>();

            PeakLists = new ObservableCollection<ObservableCollection<DataRow>>();
        }

        public void SetRawFile(IRawDataExtended rf)
        {
            _rawFile = rf;
            _numberOfScansInt = _rawFile.RunHeaderEx.SpectraCount;
            GetPDASpectra();
        }

        public double[] GetRawWavelengths()
        {
            return wavelengthsRaw;
        }

        public double[][] GetRawIntensities()
        {
            return intensityListRaw.ToArray();
        }

        private void GetPDASpectra()
        {
            double[] times = new double[_numberOfScansInt];

            // Get the first and last scan from the RAW file
            int firstScanNumber = _rawFile.RunHeaderEx.FirstSpectrum;
            int lastScanNumber = _rawFile.RunHeaderEx.LastSpectrum;

            int i = 0;
            for (int sn = firstScanNumber; sn <= lastScanNumber; sn++)
            {
                var stats = _rawFile.GetScanStatsForScanNumber(sn);
                var scan = _rawFile.GetSegmentedScanFromScanNumber(sn,stats);
                times[i] = _rawFile.RetentionTimeFromScanNumber(sn);

                if (sn == 1)
                {
                    wavelengthsRaw = scan.Positions;
                }

                var log10valuesForList = new double[scan.Intensities.Length];
                var valuesForList = new double[scan.Intensities.Length];

                for (int j = 0; j < scan.Intensities.Length; j++)
                {
                    double value = scan.Intensities[j] / 1000000;
                    
                    valuesForList[j] = value;
                    log10valuesForList[j] = scan.Intensities[j] <= 0 ? 0 : Math.Log10(scan.Intensities[j]);

                    if (value < minIntensityRaw)
                    {
                        minIntensityRaw = value;
                    }
                    if (value > maxIntensityRaw)
                    {
                        maxIntensityRaw = value;
                    }
                }
                
                scanNumbers.Add(sn);
                log10intensityListRaw.Add(log10valuesForList);
                intensityListRaw.Add(valuesForList);
                i++;
            }

            NumberOfScans = scanNumbers.Count().ToString();

            if (double.IsNaN(PlotSettings.Instance.Chromatogram.ColorMin))
            {
                PlotSettings.Instance.Chromatogram.ColorMin = minIntensityRaw;
                PlotSettings.Instance.Chromatogram.ColorMax = maxIntensityRaw;
                PlotSettings.Instance.Chromatogram.DefaultMinColorValue = minIntensityRaw;
                PlotSettings.Instance.Chromatogram.DefaultMaxColorValue = maxIntensityRaw;
            }

        }

        public void TrimDataToWavelengthRange()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            minIntensitySliced = double.MaxValue;
            maxIntensitySliced = double.MinValue;

            var min = PlotSettings.Instance.WavelengthRangeMinimum;
            var max = PlotSettings.Instance.WavelengthRangeMaximum;

            var wl = wavelengthsRaw;
            var au = intensityListRaw;
            //var times = Data.GetTimes();

            // Get the indices of values between Min and Max
            var indices = wl
                .Select((value, index) => new { value, index })
                .Where(pair => pair.value >= min && pair.value <= max)
                .Select(pair => pair.index)
                .ToArray();

            // Create a new 2D array for the selected rows
            intensityListSliced = new List<double[]>();
            log10intensityListSliced = new List<double[]>();

            wavelengthsSliced = new double[indices.Length];

            // Fill the new array with the selected rows
            for (int i = 0; i < au.Count(); i++)
            {
                double[] valuesForList = new double[indices.Length];
                double[] log10valuesForList = new double[indices.Length];

                for (int j = 0; j < indices.Length; j++)
                {
                    int columnIndex = indices[j];
                    double value = au[i][columnIndex];

                    valuesForList[j] = value;
                    log10valuesForList[j] = value <= 0 ? 0 : Math.Log10(value*1e6);

                    if (i == 0)
                    {
                        wavelengthsSliced[j] = wl[columnIndex];
                    }

                    if (au[i][columnIndex] < minIntensitySliced.GetValueOrDefault())
                    {
                        minIntensitySliced = value;
                    }
                    if (au[i][columnIndex] > maxIntensitySliced.GetValueOrDefault())
                    {
                        maxIntensitySliced = value;
                    }
                }


                intensityListSliced.Add(valuesForList);
                log10intensityListSliced.Add(log10valuesForList);

            }

            PlotSettings.Instance.Chromatogram.DefaultMinColorValue = minIntensitySliced.GetValueOrDefault();
            PlotSettings.Instance.Chromatogram.DefaultMaxColorValue = maxIntensitySliced.GetValueOrDefault();

            if (PlotSettings.Instance.Chromatogram.MapScaling == "Linear")
            {
                PlotSettings.Instance.Chromatogram.ColorMin = minIntensitySliced.GetValueOrDefault();
                PlotSettings.Instance.Chromatogram.ColorMax = maxIntensitySliced.GetValueOrDefault();
            }
            else
            {
                PlotSettings.Instance.Chromatogram.ColorMin = minIntensitySliced.GetValueOrDefault() <= 0 ? 0 : Math.Log10(minIntensitySliced.GetValueOrDefault()*1e6);
                PlotSettings.Instance.Chromatogram.ColorMax = maxIntensitySliced.GetValueOrDefault() <= 0 ? 0 : Math.Log10(maxIntensitySliced.GetValueOrDefault()*1e6);
            }

            Mouse.OverrideCursor = null;
        }

        public void ResetWavelengthRange()
        {
            intensityListSliced = null;
            log10intensityListSliced = null;
            wavelengthsSliced = null;

            minIntensitySliced = null;
            maxIntensitySliced = null;

            PlotSettings.Instance.Chromatogram.ColorMin = minIntensityRaw;
            PlotSettings.Instance.Chromatogram.ColorMax = maxIntensityRaw;
            PlotSettings.Instance.Chromatogram.DefaultMinColorValue = minIntensityRaw;
            PlotSettings.Instance.Chromatogram.DefaultMaxColorValue = maxIntensityRaw;
        }


        public ObservableCollection<DataRow> CreatePeakList(int ScanNumber)
        {
            ObservableCollection<DataRow> peaks = new ObservableCollection<DataRow>();

            for (int i = 0; i < IntensityList[ScanNumber].Count(); i++)
            {
                DataRow row = new DataRow();
                row.Mass = Wavelengths[i];
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

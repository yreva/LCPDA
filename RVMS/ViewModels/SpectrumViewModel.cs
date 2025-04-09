using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using System.Collections.Concurrent;
using System.Windows.Input;


namespace RVMS.ViewModels
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ObservableCollection<DataRow>> _peakLists;

        private double[] combinedMassesRaw;
        private double[] combinedMassesSliced;

        private List<double[]> allMassesRaw;
        private List<double[]> allMassesSliced;

        private List<double[]> intensityListSliced;
        private List<double[]> intensityListRaw;
        private List<double[]> log10intensityListSliced;
        private List<double[]> log10intensityListRaw;

        private List<double[]> intensity2dRaw;
        private List<double[]> intensity2dSliced;
        private List<double[]> log10Intensity2dRaw;
        private List<double[]> log10Intensity2dSliced;

        private double minIntensityRaw = double.MaxValue;
        private double maxIntensityRaw = double.MinValue;
        private double? minIntensitySliced = null;
        private double? maxIntensitySliced = null;

        private double[] importedWavelength;
        private double[] importedAbsorbance;

        private List<int> scanNumbers;
        private int roundToDecimal;

        private IRawDataExtended _rawFile;


        public List<double[]> IntensityList
        {
            get { return intensityListSliced ?? intensityListRaw; }
        }

        public List<double[]> Log10IntensityList
        {
            get { return log10intensityListSliced ?? log10intensityListRaw; }
        }

        public List<double[]> Intensity2D
        {
            get { return intensity2dSliced ?? intensity2dRaw; }
        }

        public List<double[]> Log10Intensity2D
        {
            get { return log10Intensity2dSliced ?? log10Intensity2dRaw; }
        }


        public double MinIntensity
        {
            get { return minIntensitySliced ?? minIntensityRaw; }
        }

        public double MaxIntensity
        {
            get { return maxIntensitySliced ?? maxIntensityRaw; }
        }

        public List<double[]> MassesList
        {
            get { return allMassesSliced ?? allMassesRaw; }
        }

        public double[] CombinedMasses
        {
            get { return combinedMassesSliced ?? combinedMassesRaw; }
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
            combinedMassesRaw = new double[0];
            intensityListRaw = new List<double[]>();
            log10intensityListRaw = new List<double[]>();

            combinedMassesSliced = null;
            intensityListSliced = null;
            log10intensityListSliced = null;

            scanNumbers = new List<int>();

            PeakLists = new ObservableCollection<ObservableCollection<DataRow>>();
        }

        public void SetRawFile(IRawDataExtended rf)
        {
            _rawFile = rf;
            _numberOfScansInt = _rawFile.RunHeaderEx.SpectraCount;
            GetMassSpectra();
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
                var scan = _rawFile.GetSegmentedScanFromScanNumber(sn);
                times[i] = _rawFile.RetentionTimeFromScanNumber(sn);

                if (sn == 1)
                {
                    //wavelengthsRaw = scan.Positions;
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

        public void SetMassResolution(int mrd)
        {
            roundToDecimal = mrd;
            return;
        }

        private async void GetMassSpectra()
        {
            List<int> massSpectraIdx = new List<int>();

            allMassesRaw = new List<double[]>();
            intensityListRaw = new List<double[]>();
            scanNumbers = new List<int>();

            // Get the first and last scan from the RAW file
            int firstScanNumber = _rawFile.RunHeaderEx.FirstSpectrum;
            int lastScanNumber = _rawFile.RunHeaderEx.LastSpectrum;

            for (int i = 1; i <= lastScanNumber; i++)
            {
                var time = _rawFile.RetentionTimeFromScanNumber(i);

                var scanFilter = _rawFile.GetFilterForScanNumber(i);
                var scanEvent = _rawFile.GetScanEventForScanNumber(i);

                if (scanFilter.MSOrder != ThermoFisher.CommonCore.Data.FilterEnums.MSOrderType.Ms)
                {
                    continue;
                }

                massSpectraIdx.Add(i);

                var scanStatistics = _rawFile.GetScanStatsForScanNumber(i);

                if (scanStatistics.IsCentroidScan)
                {
                    continue;
                }

                // else scan is segmented - what we want?

                SegmentedScan ss = _rawFile.GetSegmentedScanFromScanNumber(i, scanStatistics);

                List<(double Mass, double Intensity)> data = ss.Positions.Zip(ss.Intensities, (m, j) => (m, j)).ToList();

                // this rounds and adds to the mz,intensity lists...
                RoundMassesToDecimal(data, roundToDecimal);

                scanNumbers.Add(i);
            }

            Polarity = _rawFile.GetFilterForScanNumber(scanNumbers[0]).Polarity.ToString();
            NumberOfScans = scanNumbers.Count().ToString();

            var result = await ProcessIntensityMatrixAsync(allMassesRaw, intensityListRaw, scanNumbers.Count());

            combinedMassesRaw = result.Item1.ToArray();
            intensity2dRaw = result.Item2;
            log10Intensity2dRaw = result.Item3;

        }

        private void RoundMassesToDecimal(List<(double Mass, double Intensity)> data, int toDecimal)
        {
            var groupedData = data
                .GroupBy(d => Math.Round(d.Mass, toDecimal))
                .Select(g => (Mass: g.Key, Intensity: g.Max(d => d.Intensity)))
                .ToList();

            allMassesRaw.Add(groupedData.Select(item => item.Mass).ToArray());
            intensityListRaw.Add(groupedData.Select(item => item.Intensity).ToArray());
        }

        public async Task<(List<double>, List<double[]>, List<double[]>)> ProcessIntensityMatrixAsync(
            List<double[]> massLists, List<double[]> intensityLists, int numScans)
        {
            // Step 1: Find all unique mz values
            HashSet<double> uniqueMzSet = new HashSet<double>(massLists.SelectMany(x => x));
            List<double> uniqueMZ = uniqueMzSet.OrderBy(x => x).ToList();

            // Step 2: Initialize intensity matrix
            int numRows = uniqueMZ.Count;
            List<double[]> intensityVsTime = new List<double[]>(new double[numScans][]);
            List<double[]> logIntensityVsTime = new List<double[]>(new double[numScans][]);

            Parallel.For(0, numScans, i =>
            {
                intensityVsTime[i] = new double[numRows];
                logIntensityVsTime[i] = new double[numRows];
            });

            // Step 3: Use a ConcurrentDictionary to store intensity data
            ConcurrentDictionary<int, double[]> intensityStorage = new ConcurrentDictionary<int, double[]>();

            // Step 4: Build a mapping from mz to row index for fast lookup
            Dictionary<double, int> mzIndexMap = uniqueMZ
                .Select((value, index) => new { value, index })
                .ToDictionary(x => x.value, x => x.index);

            // Step 5: Use Parallel.ForEach to process scans in parallel
            await Task.Run(() =>
            {
                Parallel.For(0, massLists.Count(), scanIdx =>
                {
                    double[] mzScan = massLists[scanIdx];
                    double[] intensityScan = intensityLists[scanIdx];

                    foreach ((double mz, double intensity) in mzScan.Zip(intensityScan, (m, i) => (m, i)))
                    {
                        if (mzIndexMap.TryGetValue(mz, out int rowIndex))
                        {
                            // Store intensity data in ConcurrentDictionary
                            intensityStorage.AddOrUpdate(rowIndex,
                                _ => new double[numScans], // Initialize new row
                                (_, existingRow) =>
                                {
                                    existingRow[scanIdx] = intensity;
                                    return existingRow;
                                }
                            );
                        }
                    }
                });
            });

            // Step 6: Copy from ConcurrentDictionary to final double[,]
            foreach (var kvp in intensityStorage)
            {
                int rowIndex = kvp.Key;
                double[] rowValues = kvp.Value;
                for (int col = 0; col < numScans; col++)
                {
                    intensityVsTime[col][rowIndex] = rowValues[col];
                    logIntensityVsTime[col][rowIndex] = (rowValues[col] == 0) ? 0 : Math.Log10(rowValues[col]);

                    if (rowValues[col] < minIntensityRaw)
                    {
                        minIntensityRaw = rowValues[col];
                    }

                    if (rowValues[col] > maxIntensityRaw)
                    {
                        maxIntensityRaw = rowValues[col];
                    }
                }
            }

            MessageBox.Show(Application.Current.MainWindow, "Processing Finished, File Loaded.", "Loading Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);

            PlotSettings.Instance.Chromatogram.DefaultMinColorValue = minIntensityRaw;
            PlotSettings.Instance.Chromatogram.DefaultMaxColorValue = maxIntensityRaw;

            PlotSettings.Instance.Chromatogram.ColorMin = minIntensityRaw;
            PlotSettings.Instance.Chromatogram.ColorMax = maxIntensityRaw;

            return (uniqueMZ, intensityVsTime, logIntensityVsTime);
        }

        public void TrimDataToWavelengthRange()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            minIntensitySliced = double.MaxValue;
            maxIntensitySliced = double.MinValue;

            var min = PlotSettings.Instance.WavelengthRangeMinimum;
            var max = PlotSettings.Instance.WavelengthRangeMaximum;

            var wl = combinedMassesRaw;
            var au = intensity2dRaw;
            //var times = Data.GetTimes();

            // Get the indices of values between Min and Max
            var indices = wl
                .Select((value, index) => new { value, index })
                .Where(pair => pair.value >= min && pair.value <= max)
                .Select(pair => pair.index)
                .ToArray();

            // Create a new 2D array for the selected rows
            intensity2dSliced = new List<double[]>();
            log10Intensity2dSliced = new List<double[]>();

            combinedMassesSliced = new double[indices.Length];

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
                    log10valuesForList[j] = value <= 0 ? 0 : Math.Log10(value);

                    if (i == 0)
                    {
                        combinedMassesSliced[j] = wl[columnIndex];
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


                intensity2dSliced.Add(valuesForList);
                log10Intensity2dSliced.Add(log10valuesForList);

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
            allMassesSliced = null;
            intensityListSliced = null;
            log10intensityListSliced = null;

            combinedMassesSliced = null;
            intensity2dSliced = null;
            log10Intensity2dSliced = null;

            minIntensitySliced = null;
            maxIntensitySliced = null;

            PlotSettings.Instance.Chromatogram.ColorMin = minIntensityRaw;
            PlotSettings.Instance.Chromatogram.ColorMax = maxIntensityRaw;
            PlotSettings.Instance.Chromatogram.DefaultMinColorValue = minIntensityRaw;
            PlotSettings.Instance.Chromatogram.DefaultMaxColorValue = maxIntensityRaw;
        }


        public ObservableCollection<DataRow> CreatePeakList(int scanIndex)
        {
            ObservableCollection<DataRow> peaks = new ObservableCollection<DataRow>();

            for (int i = 0; i < IntensityList[scanIndex].Count(); i++)
            {
                DataRow row = new DataRow();
                row.Mass = MassesList[scanIndex][i];
                row.Intensity = IntensityList[scanIndex][i];
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

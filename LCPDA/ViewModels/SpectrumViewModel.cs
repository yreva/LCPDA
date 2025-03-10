using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;
using System.Collections.Concurrent;


namespace RawVision.ViewModels
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ObservableCollection<DataRow>> _peakLists;
        private List<double> uniqueMasses;
        private double[,] _intensities2D;
        private double[,] _log10Intensities2D;
        private List<double[]> mz;
        private List<double[]> intensity;
        private List<int> scanNumbers;
        private int roundToDecimal;

        private IRawDataExtended _rawFile;

        public List<double[]> MZ
        {
            get { return mz; }
        }

        public List<double[]> Intensity
        {
            get { return intensity; }
        }

        public List<double> UniqueMasses
        {
            get { return uniqueMasses; }
        }

        public double[,] Intensities2D
        {
            get { return _intensities2D; }
        }

        public double[,] Log10Intensities2D
        {
            get { return _log10Intensities2D; }
        }

        public ObservableCollection<ObservableCollection<DataRow>> PeakLists
        {
            get => _peakLists;
            set { _peakLists = value; }
        }

        public SpectrumViewModel()
        {
            mz = new List<double[]>();
            intensity = new List<double[]>();
            scanNumbers = new List<int>();

            PeakLists = new ObservableCollection<ObservableCollection<DataRow>>();
        }

        public void SetRawFile(IRawDataExtended rf)
        {
            _rawFile = rf;
            GetMassSpectra();
        }

        public void SetMassResolution(int mrd)
        {
            roundToDecimal = mrd;
            return;
        }

        private async void GetMassSpectra()
        {
            List<int> massSpectraIdx = new List<int>();

            mz = new List<double[]>();
            intensity = new List<double[]>();
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
            }

            List<double[]> massList = new List<double[]>();
            List<double[]> massIntensities = new List<double[]>();

            foreach (int i in massSpectraIdx)
            {
                var scanStatistics = _rawFile.GetScanStatsForScanNumber(i);

                if (scanStatistics.IsCentroidScan)
                {
                    continue;
                }

                // else scan is segmented - what we want?

                SegmentedScan ss = _rawFile.GetSegmentedScanFromScanNumber(i, scanStatistics);

                List<(double Mass, double Intensity)> data = ss.Positions.Zip(ss.Intensities, (m, j) => (m, j)).ToList();

                // this rounds and adds to the mz,intensity lists...
                RoundMassesToDecimal(data,roundToDecimal);

                scanNumbers.Add(i);
            }

            var result = await ProcessIntensityMatrixAsync(mz, intensity, scanNumbers.Count());

            uniqueMasses = result.Item1;
            _intensities2D = result.Item2;
            _log10Intensities2D = result.Item3;
        }


        private void RoundMassesToDecimal(List<(double Mass, double Intensity)> data, int toDecimal)
        {
            var groupedData = data
                        .GroupBy(d => Math.Round(d.Mass, toDecimal))
                        .Select(g => (Mass: g.Key, Intensity: g.Max(d => d.Intensity)))
                        .ToList();

            mz.Add(groupedData.Select(item => item.Mass).ToArray());
            intensity.Add(groupedData.Select(item => item.Intensity).ToArray());
        }

        public static async Task<(List<double>, double[,], double[,])> ProcessIntensityMatrixAsync(
                List<double[]> massLists, List<double[]> intensityLists, int numScans)
        {
            // Step 1: Find all unique mz values
            HashSet<double> uniqueMzSet = new HashSet<double>(massLists.SelectMany(x => x));
            List<double> uniqueMZ = uniqueMzSet.OrderBy(x => x).ToList();

            // Step 2: Initialize intensity matrix
            int numRows = uniqueMZ.Count;
            double[,] intensityVsTime = new double[numRows, numScans];
            double[,] logIntensityVsTime = new double[numRows, numScans];


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
                    intensityVsTime[rowIndex, col] = rowValues[col];
                    logIntensityVsTime[rowIndex, col] = (rowValues[col] == 0) ? 0 : Math.Log10(rowValues[col]);
                }
            }

            MessageBox.Show("Processing Finished, File Loaded.", "Loading Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            return (uniqueMZ, intensityVsTime, logIntensityVsTime);
        }

        public ObservableCollection<DataRow> CreatePeakList(int ScanNumber)
        {
            ObservableCollection<DataRow> peaks = new ObservableCollection<DataRow>();

            for (int i = 0; i < mz[ScanNumber].Count(); i++)
            {
                DataRow row = new DataRow();
                row.Mass = mz[ScanNumber][i];
                row.Intensity = intensity[ScanNumber][i];
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

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
        private ObservableCollection<Point> _dataPoints;
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

        public SpectrumViewModel()
        {
            mz = new List<double[]>();
            intensity = new List<double[]>();
            scanNumbers = new List<int>();
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
                                (_, existingRow) => { existingRow[scanIdx] = intensity; return existingRow; }
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

            MessageBox.Show("Processing Finished!");

            return (uniqueMZ, intensityVsTime, logIntensityVsTime);
        }

        public (List<double>, double[,]) ProcessAllScansOld(List<double[]> massLists, List<double[]> intensityLists, int numScans)
        {
            // Step 1: Find all unique mz values
            HashSet<double> uniqueMzSet = new HashSet<double>(mz.SelectMany(x => x));
            List<double> uniqueMZ = uniqueMzSet.OrderBy(x => x).ToList();

            // Step 2: Initialize intensity matrix (rows = uniqueMZ, columns = scans)
            double[,] intensityVsTime = new double[uniqueMZ.Count(),numScans-1];

            // Step 3: Populate intensity values
            for (int scanIdx = 0; scanIdx < mz.Count; scanIdx++)
            {
                double[] mzScan = mz[scanIdx];
                double[] intensityScan = intensity[scanIdx];

                for (int i = 0; i < mzScan.Length; i++)
                {
                    double currentMz = mzScan[i];
                    double currentIntensity = intensityScan[i];

                    // Find the index of the currentMz in uniqueMZ
                    int rowIndex = uniqueMZ.IndexOf(currentMz);  // Could be optimized with a dictionary
                    if (rowIndex != -1)
                        intensityVsTime[rowIndex,scanIdx] = currentIntensity;
                }
            }

            return (uniqueMZ, intensityVsTime);
        }

        private void ProcessMassSpectra1(List<(double Mass, double Intensity)> data)
        {
            double tolerance = 0.0001; // Define tolerance for grouping

            // Sort by mass to ensure correct grouping
            data = data.OrderBy(d => d.Mass).ToList();

            // List to store grouped data
            List<(double GroupedMass, double TotalIntensity)> groupedData = new();

            double currentGroupMass = data[0].Mass;
            double intensitySum = 0;
            int count = 0;

            foreach (var (mass, intensity) in data)
            {
                // If the mass is close enough to the current group, add to the same cluster
                if (Math.Abs(mass - currentGroupMass) <= tolerance)
                {
                    intensitySum += intensity;
                    count++;
                }
                else
                {
                    // Store the previous cluster
                    groupedData.Add((currentGroupMass, intensitySum/count));

                    // Start a new cluster
                    currentGroupMass = mass;
                    intensitySum = intensity;
                    count = 1;
                }
            }

            // Add the last group
            groupedData.Add((currentGroupMass, intensitySum));

            mz.Add(groupedData.Select(item => item.GroupedMass).ToArray());
            intensity.Add(groupedData.Select(item => item.TotalIntensity).ToArray());

        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

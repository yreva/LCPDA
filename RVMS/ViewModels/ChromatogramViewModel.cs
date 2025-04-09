using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Formats.Tar;
using System.Windows;
using ScottPlot;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.Interfaces;

namespace RVMS.ViewModels
{
    public class ChromatogramViewModel : INotifyPropertyChanged
    {

        private IRawDataExtended _rawFile;
        private double[] times;
        private double[] tic;


        public ChromatogramViewModel()
        {
        }

        public void SetRawFile(IRawDataExtended rf)
        {
            _rawFile = rf;
            GetChromatogram();
        }

        private void GetChromatogram()
        {
            // Get the first and last scan from the RAW file
            int firstScanNumber = _rawFile.RunHeaderEx.FirstSpectrum;
            int lastScanNumber = _rawFile.RunHeaderEx.LastSpectrum;

            // Define the settings for getting the Base Peak chromatogram
            ChromatogramTraceSettings settings = new ChromatogramTraceSettings(TraceType.BasePeak);
            settings.Filter = "MS";

            // Get the chromatogram from the RAW file. 
            var data = _rawFile.GetChromatogramData(new IChromatogramSettings[] { settings }, firstScanNumber, lastScanNumber);

            // Split the data into the chromatograms
            var trace = ChromatogramSignal.FromChromatogramData(data);

            if (trace[0].Length > 0)
            {
                // Print the chromatogram data (time, intensity values)
                Console.WriteLine("Base Peak chromatogram ({0} points)", trace[0].Length);
            }

            times = trace[0].Times.ToArray();
            tic = trace[0].Intensities.ToArray();

            Console.WriteLine();
        }

        public double[] Times
        {
            get { return times; }
        }

        public double[] TIC
        {
            get { return tic; }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

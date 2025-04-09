using System;
using System.IO;
using System.Windows;
using ThermoFisher.CommonCore.Data;
using ThermoFisher.CommonCore.Data.Business;
using ThermoFisher.CommonCore.Data.FilterEnums;
using ThermoFisher.CommonCore.Data.Interfaces;
using ThermoFisher.CommonCore.MassPrecisionEstimator;
using ThermoFisher.CommonCore.RawFileReader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RVMS.Models
{
    public class IOModel
    {
        private IRawDataExtended _rawFile;
        private string _filePath;

        public IOModel()
        {
        }

        public int OpenRawFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("No RAW file specified!");

                return 0;
            }

            // Check to see if the specified RAW file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine();
                MessageBox.Show(@"The file doesn't exist in the specified location - " + filePath, "File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }

            // Create the IRawDataPlus object for accessing the RAW file
            var rawFile = RawFileReaderAdapter.FileFactory(filePath);

            if (!rawFile.IsOpen)
            {
                Console.WriteLine("Unable to access the RAW file using the RawFileReader class!");
                MessageBox.Show("The file could not be opened - reason unknown.", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }

            // Check for any errors in the RAW file
            if (rawFile.IsError)
            {
                Console.WriteLine("Error opening ({0}) - {1}", rawFile.FileError, filePath);
                MessageBox.Show(string.Format("Error opening ({0}) - {1}", filePath, rawFile.FileError), "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }

            // Check if the RAW file is being acquired
            if (rawFile.InAcquisition)
            {
                Console.WriteLine("RAW file still being acquired - " + filePath);
                MessageBox.Show("Please wait until the file is done being acquired.", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return 0;
            }

            _filePath = filePath;

            // Get the number of instruments (controllers) present in the RAW file and set the 
            // selected instrument to the MS instrument, first instance of it
            Console.WriteLine("The RAW file has data from {0} instruments" + rawFile.InstrumentCount);

            rawFile.SelectInstrument(Device.MS, 1);

            // Get the first and last scan from the RAW file
            int firstScanNumber = rawFile.RunHeaderEx.FirstSpectrum;
            int lastScanNumber = rawFile.RunHeaderEx.LastSpectrum;

            // Get the start and end time from the RAW file
            double startTime = rawFile.RunHeaderEx.StartTime;
            double endTime = rawFile.RunHeaderEx.EndTime;

            // Print some OS and other information
            Console.WriteLine("System Information:");
            Console.WriteLine("   OS Version: " + Environment.OSVersion);
            Console.WriteLine("   64 bit OS: " + Environment.Is64BitOperatingSystem);
            Console.WriteLine("   Computer: " + Environment.MachineName);
            Console.WriteLine("   # Cores: " + Environment.ProcessorCount);
            Console.WriteLine("   Date: " + DateTime.Now);
            Console.WriteLine();

            // Get some information from the header portions of the RAW file and display that information.
            // The information is general information pertaining to the RAW file.
            Console.WriteLine("General File Information:");
            Console.WriteLine("   RAW file: " + rawFile.FileName);
            Console.WriteLine("   RAW file version: " + rawFile.FileHeader.Revision);
            Console.WriteLine("   Creation date: " + rawFile.FileHeader.CreationDate);
            Console.WriteLine("   Operator: " + rawFile.FileHeader.WhoCreatedId);
            Console.WriteLine("   Number of instruments: " + rawFile.InstrumentCount);
            Console.WriteLine("   Description: " + rawFile.FileHeader.FileDescription);
            Console.WriteLine("   Instrument model: " + rawFile.GetInstrumentData().Model);
            Console.WriteLine("   Instrument name: " + rawFile.GetInstrumentData().Name);
            Console.WriteLine("   Serial number: " + rawFile.GetInstrumentData().SerialNumber);
            Console.WriteLine("   Software version: " + rawFile.GetInstrumentData().SoftwareVersion);
            Console.WriteLine("   Firmware version: " + rawFile.GetInstrumentData().HardwareVersion);
            Console.WriteLine("   Units: " + rawFile.GetInstrumentData().Units);
            Console.WriteLine("   Mass resolution: {0:F3} ", rawFile.RunHeaderEx.MassResolution);
            Console.WriteLine("   Number of scans: {0}", rawFile.RunHeaderEx.SpectraCount);
            Console.WriteLine("   Scan range: {0} - {1}", firstScanNumber, lastScanNumber);
            Console.WriteLine("   Time range: {0:F2} - {1:F2}", startTime, endTime);
            Console.WriteLine("   Mass range: {0:F4} - {1:F4}", rawFile.RunHeaderEx.LowMass, rawFile.RunHeaderEx.HighMass);
            Console.WriteLine();

            // Get information related to the sample that was processed
            Console.WriteLine("Sample Information:");
            Console.WriteLine("   Sample name: " + rawFile.SampleInformation.SampleName);
            Console.WriteLine("   Sample id: " + rawFile.SampleInformation.SampleId);
            Console.WriteLine("   Sample type: " + rawFile.SampleInformation.SampleType);
            Console.WriteLine("   Sample comment: " + rawFile.SampleInformation.Comment);
            Console.WriteLine("   Sample vial: " + rawFile.SampleInformation.Vial);
            Console.WriteLine("   Sample volume: " + rawFile.SampleInformation.SampleVolume);
            Console.WriteLine("   Sample injection volume: " + rawFile.SampleInformation.InjectionVolume);
            Console.WriteLine("   Sample row number: " + rawFile.SampleInformation.RowNumber);
            Console.WriteLine("   Sample dilution factor: " + rawFile.SampleInformation.DilutionFactor);
            Console.WriteLine();

            // Read the first instrument method (most likely for the MS portion of the instrument).
            // NOTE: This method reads the instrument methods from the RAW file but the underlying code
            // uses some Microsoft code that hasn't been ported to Linux or MacOS.  Therefore this
            // method won't work on those platforms therefore the check for Windows.
            if (Environment.OSVersion.ToString().Contains("Windows"))
            {
                var deviceNames = rawFile.GetAllInstrumentNamesFromInstrumentMethod();

                foreach (var device in deviceNames)
                {
                    Console.WriteLine("Instrument name: " + device);
                }

                Console.WriteLine();
            }

            _rawFile = rawFile;

            return 1;
        }

        public IRawDataExtended GetRawFileFromIOModel()
        {
            return _rawFile;
        }

        public static (double[], double[]) LoadCsvColumns(string filePath)
        {
            // 1) Check if file exists
            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV file not found.");

            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
                throw new InvalidDataException("CSV file is empty.");

            // Determine if there's a header
            bool hasHeader = lines[0].Split(',').All(x => !double.TryParse(x, out _));
            var dataLines = hasHeader ? lines.Skip(1) : lines;

            // Read the CSV data
            var parsedData = dataLines
                .Select(line => line.Split(','))
                .ToList();

            // 2) Ensure exactly 2 columns
            if (parsedData.Any(columns => columns.Length != 2))
                throw new InvalidDataException("CSV file must have exactly 2 columns.");

            // 3) Ensure all values are integers
            if (!parsedData.All(columns => double.TryParse(columns[0], out _) && double.TryParse(columns[1], out _)))
                throw new InvalidDataException("All non-header values must be integers.");

            // 4) Extract and convert data
            double[] column1 = parsedData.Select(columns => double.Parse(columns[0])).ToArray();
            double[] column2 = parsedData.Select(columns => double.Parse(columns[1])).ToArray();

            // 5) Ensure both columns have the same length
            if (column1.Length != column2.Length)
                throw new InvalidDataException("Columns must have the same number of values.");

            return (column1, column2);
        }

        public void WriteDataToCsv(double[] masses, double[] times, double[][] intensity)
        {
            if (masses == null)
            {
                MessageBox.Show("No data appears to be loaded.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var a = _filePath;

            var m = a.ToUpper().Replace(".RAW","_Masses.csv");
            var t = a.ToUpper().Replace(".RAW","_Times.csv");
            var i = a.ToUpper().Replace(".RAW", "_Spectra.csv");

            using (StreamWriter writer = new StreamWriter(i))
            {
                int rows = intensity.GetLength(0);
                int cols = intensity.GetLength(1);

                for (int j = 0; j < rows; j++)
                {
                    string[] rowValues = new string[cols];
                    for (int k = 0; k < cols; k++)
                    {
                        rowValues[k] = intensity[j][k].ToString();
                    }
                    writer.WriteLine(string.Join(",", rowValues));
                }
            }

            using (StreamWriter writer = new StreamWriter(m))
            {

                foreach (double value in masses)
                {
                    writer.WriteLine(value);
                }
            }

            using (StreamWriter writer = new StreamWriter(t))
            {

                foreach (double value in times)
                {
                    writer.WriteLine(value);
                }
            }
        }
    }
}
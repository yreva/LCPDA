using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThermoFisher.CommonCore.Data.Business;

namespace RawVision
{
    public class PlotSettings : INotifyPropertyChanged
    {
        // Implement PropertyChanged methods
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Static instance, initialized once
        private static readonly PlotSettings _instance = new PlotSettings();

        // Public property to access the singleton instance
        public static PlotSettings Instance => _instance;

        // Private constructor to prevent instantiation from outside
        private PlotSettings()
        {
            // Default settings initialization
            Theme = "Light";
            ChromatogramStyle = "Line";
            ChromatogramMapScaling = "Linear";
            ScanNumber = 1;
        }

        // Example settings
        public string Theme { get; set; } // "Light" or "Dark"
        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }

        private string _chromatogramStyle;
        public string ChromatogramStyle
        {
            get { return _chromatogramStyle; }
            set
            {
                _chromatogramStyle = value;
                OnPropertyChanged(nameof(ChromatogramStyle));
            }
        }

        private string _chromatogramMapScaling;
        public string ChromatogramMapScaling
        {
            get { return _chromatogramMapScaling; }
            set
            {
                _chromatogramMapScaling = value;
                OnPropertyChanged(nameof(ChromatogramMapScaling));
            }
        }

        private int _scanNumber;
        public int ScanNumber
        {
            get { return _scanNumber; }
            set
            {
                _scanNumber = value;
                OnPropertyChanged(nameof(ScanNumber));
            }
        }

    }
}

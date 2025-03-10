using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ScottPlot;
using ThermoFisher.CommonCore.Data.Business;
using Color = System.Drawing.Color;

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

        public ChromatogramSettings Chromatogram;
        public SpectrumSettings Spectrum;

        // Private constructor to prevent instantiation from outside
        private PlotSettings()
        {
            ScanNumber = 1;

            Chromatogram = new ChromatogramSettings();
            Spectrum = new SpectrumSettings();

        }

        // Example settings
        public string Theme { get; set; } // "Light" or "Dark"

        private int _scanNumber;
        public int ScanNumber
        {
            get { return _scanNumber; }
            set
            {
                if (value == 0)
                {
                    _scanNumber = 1;
                    OnPropertyChanged(nameof(ScanNumber));
                    return;
                }
                _scanNumber = value;
                OnPropertyChanged(nameof(ScanNumber));
            }
        }

    }

    public class ChromatogramSettings : BaseSettings
    {
        public ChromatogramSettings()
        {
            XMin = 0;
            YMin = 0;
            XMax = 0;
            YMax = 0;
            AutoScaleX = 0;
            AutoScaleY = 0;
            LineColor = ScottPlot.Color.FromSDColor(Color.MidnightBlue);
            MouseEventsEnabled = true;
            GridEnabled = true;
            VLineEnabled = true;
        }

        private double _xMin;
        private double _xMax;
        private double _yMin;
        private double _yMax;

        private bool _mouseEventsEnabled;
        private bool _gridEnabled;
        private bool _vLineEnabled;

        public double XMin
        {
            get => _xMin;
            set => SetProperty(ref _xMin, value, nameof(XMin));
        }

        public double XMax
        {
            get => _xMax;
            set => SetProperty(ref _xMax, value, nameof(XMax));
        }

        public double YMin
        {
            get => _yMin;
            set => SetProperty(ref _yMin, value, nameof(YMin));
        }

        public double YMax
        {
            get => _yMax;
            set => SetProperty(ref _yMax, value, nameof(YMax));
        }

        public bool MouseEventsEnabled
        {
            get => _mouseEventsEnabled;
            set => SetProperty(ref _mouseEventsEnabled, value, nameof(MouseEventsEnabled));
        }

        public bool GridEnabled
        {
            get => _mouseEventsEnabled;
            set => SetProperty(ref _mouseEventsEnabled, value, nameof(GridEnabled));
        }

        public bool VLineEnabled
        {
            get => _mouseEventsEnabled;
            set => SetProperty(ref _mouseEventsEnabled, value, nameof(VLineEnabled));
        }

        private int _autoScaleX;
        public int AutoScaleX
        {
            get => _autoScaleX;
            set => SetProperty(ref _autoScaleX, value, nameof(AutoScaleX));
        }


        private int _autoScaleY;
        public int AutoScaleY
        {
            get => _autoScaleY;
            set => SetProperty(ref _autoScaleY, value, nameof(AutoScaleY));
        }

        private string _style;
        public string Style
        {
            get { return _style; }
            set
            {
                SetProperty(ref _style, value, nameof(Style));
            }
        }

        private string _mapScaling;
        public string MapScaling
        {
            get { return _mapScaling; }
            set
            {
                SetProperty(ref _mapScaling, value, nameof(MapScaling));
            }
        }

        private ScottPlot.Color _lineColor;
        public ScottPlot.Color LineColor
        {
            get { return _lineColor; }
            set => SetProperty(ref _lineColor, value, nameof(LineColor));
        }

    }

    public class SpectrumSettings : BaseSettings
    {
        public SpectrumSettings()
        {
            XMin = 0;
            YMin = 0;
            XMax = 0;
            YMax = 0;
            AutoScaleX = true;
            AutoScaleY = true;
        }


        private double _xMin;
        private double _xMax;
        private double _yMin;
        private double _yMax;

        public double XMin
        {
            get => _xMin;
            set => SetProperty(ref _xMin, value, nameof(XMin));
        }

        public double XMax
        {
            get => _xMax;
            set => SetProperty(ref _xMax, value, nameof(XMax));
        }

        public double YMin
        {
            get => _yMin;
            set => SetProperty(ref _yMin, value, nameof(YMin));
        }

        public double YMax
        {
            get => _yMax;
            set => SetProperty(ref _yMax, value, nameof(YMax));
        }

        private bool _autoScaleX;
        public bool AutoScaleX
        {
            get => _autoScaleX;
            set => SetProperty(ref _autoScaleX, value, nameof(AutoScaleX));
        }


        private bool _autoScaleY;
        public bool AutoScaleY
        {
            get => _autoScaleY;
            set => SetProperty(ref _autoScaleY, value, nameof(AutoScaleY));
        }
    }

    public class BaseSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }
    }
}

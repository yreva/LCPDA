using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using RawVision.Views;
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
            MassRangeMinimum = 0;
            MassRangeMaximum = 0;
            MassRangeLimitEnabled = false;

            Chromatogram = new ChromatogramSettings();
            Spectrum = new SpectrumSettings();
        }

        private int _scanNumber;
        public int ScanNumber
        {
            get { return _scanNumber; }
            set
            {
                if (value <= 0)
                {
                    _scanNumber = 1;
                    OnPropertyChanged(nameof(ScanNumber));
                    return;
                }
                _scanNumber = value;
                OnPropertyChanged(nameof(ScanNumber));
            }
        }

        private bool _massRangeLimitEnabled;
        public bool MassRangeLimitEnabled
        {
            get { return _massRangeLimitEnabled; }
            set
            {
                _massRangeLimitEnabled = value;
                OnPropertyChanged(nameof(MassRangeLimitEnabled));
            }
        }

        private double _massRangeMinimum;
        public double MassRangeMinimum
        {
            get { return _massRangeMinimum; }
            set
            {
                _massRangeMinimum = value;
                OnPropertyChanged(nameof(MassRangeMinimum));
            }
        }

        private double _massRangeMaximum;
        public double MassRangeMaximum
        {
            get { return _massRangeMaximum; }
            set
            {
                _massRangeMaximum = value;
                OnPropertyChanged(nameof(MassRangeMaximum));
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
            ColorMin = double.NaN;
            ColorMax = double.NaN;
            AutoScaleX = 0;
            AutoScaleY = 0;
            LineColor = ScottPlot.Color.FromSDColor(Color.MidnightBlue);
            MapScaling = "Linear";
            Style = "Line";
            MouseEventsEnabled = true;
            GridEnabled = true;
            VLineEnabled = true;
        }

        private double _xMin;
        private double _xMax;
        private double _yMin;
        private double _yMax;
        private double _colorMin;
        private double _colorMax;

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

        public double ColorMin
        {
            get => _colorMin;
            set => SetProperty(ref _colorMin, value, nameof(ColorMin));
        }

        public double ColorMax
        {
            get => _colorMax;
            set => SetProperty(ref _colorMax, value, nameof(ColorMax));
        }

        public bool MouseEventsEnabled
        {
            get => _mouseEventsEnabled;
            set => SetProperty(ref _mouseEventsEnabled, value, nameof(MouseEventsEnabled));
        }

        public bool GridEnabled
        {
            get => _gridEnabled;
            set => SetProperty(ref _gridEnabled, value, nameof(GridEnabled));
        }

        public bool VLineEnabled
        {
            get => _vLineEnabled;
            set => SetProperty(ref _vLineEnabled, value, nameof(VLineEnabled));
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

        private int _autoScaleColor;
        public int AutoScaleColor
        {
            get => _autoScaleColor;
            set => SetProperty(ref _autoScaleColor, value, nameof(AutoScaleColor));
        }

        private double _defaultMinColorValue;
        public double DefaultMinColorValue
        {
            get => _defaultMinColorValue;
            set => SetProperty(ref _defaultMinColorValue, value, nameof(DefaultMinColorValue));
        }

        private double _defaultMaxColorValue;
        public double DefaultMaxColorValue
        {
            get => _defaultMaxColorValue;
            set => SetProperty(ref _defaultMaxColorValue, value, nameof(DefaultMaxColorValue));
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
            AutoScaleX = 0;
            AutoScaleY = 0;
            BarColor = ScottPlot.Color.FromSDColor(Color.MidnightBlue);
            MouseEventsEnabled = true;
            GridEnabled = true;
            HoldManualLimits = false;
        }

        private double _xMin;
        private double _xMax;
        private double _yMin;
        private double _yMax;
        private bool _mouseEventsEnabled;
        private bool _gridEnabled;
        private bool _holdManualLimits;

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

        public bool MouseEventsEnabled
        {
            get => _mouseEventsEnabled;
            set => SetProperty(ref _mouseEventsEnabled, value, nameof(MouseEventsEnabled));
        }

        public bool GridEnabled
        {
            get => _gridEnabled;
            set => SetProperty(ref _gridEnabled, value, nameof(GridEnabled));
        }

        
        public bool HoldManualLimits
        {
            get => _holdManualLimits;
            set => SetProperty(ref _holdManualLimits, value, nameof(HoldManualLimits));
        }

        private ScottPlot.Color _barColor;
        public ScottPlot.Color BarColor
        {
            get { return _barColor; }
            set => SetProperty(ref _barColor, value, nameof(BarColor));
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

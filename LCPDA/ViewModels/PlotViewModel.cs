using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RawVision.ViewModels
{
    public class PlotViewModel
    {
        private bool _autoX_chromatogram;
        public bool AutoX_Chromatogram
        {
            get { return _autoX_chromatogram; }
            set { _autoX_chromatogram = value; }
        }

        private bool _autoY_chromatogram;
        public bool AutoY_Chromatogram
        {
            get { return _autoY_chromatogram; }
            set { _autoY_chromatogram = value; }
        }

        private double _minX_chromatogram;
        public double MinX_chromatogram
        {
            get { return _minX_chromatogram; }
            set { _minX_chromatogram = value; }
        }

        private double _minY_chromatogram;
        public double MinY_chromatogram
        {
            get { return _minY_chromatogram; }
            set { _minY_chromatogram = value; }
        }

        private double _maxX_chromatogram;
        public double MaxX_chromatogram
        {
            get { return _maxX_chromatogram; }
            set { _minX_chromatogram = value; }
        }

        private double _maxY_chromatogram;
        public double MaxY_chromatogram
        {
            get { return _maxY_chromatogram; }
            set { _maxY_chromatogram = value; }
        }

    }
}

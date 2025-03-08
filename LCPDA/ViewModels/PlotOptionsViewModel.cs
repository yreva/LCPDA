using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RawVision.ViewModels
{
    public class PlotOptionsViewModel
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
    }
}

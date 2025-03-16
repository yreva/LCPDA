using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RVPDA
{
    public class Tools
    {
        public static double RoundToSignificantFigures(double value, int sigFigures)
        {
            if (value == 0)
                return 0;

            double scale = Math.Pow(10, sigFigures - Math.Ceiling(Math.Log10(Math.Abs(value))));
            return Math.Round(value * scale) / scale;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCPDA
{
    public class ToolbarSettings
    {
        // Static instance, initialized once
        private static readonly ToolbarSettings _instance = new ToolbarSettings();

        // Public property to access the singleton instance
        public static ToolbarSettings Instance => _instance;

        // Private constructor to prevent instantiation from outside
        private ToolbarSettings()
        {
            // Default settings initialization
            Theme = "Light";
        }

        // Example settings
        public string Theme { get; set; } // "Light" or "Dark"


    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LCPDA
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
            this.Closing += Window_Closing;
        }

        // Prevent closing the window manually
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true; // Block closing until processing is done
        }

        // Method to allow closing when processing is complete
        public void CloseWindow()
        {
            this.Closing -= Window_Closing; // Remove event to allow closing
            this.Close();
        }
    }
}

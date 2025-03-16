using System;
using System.Collections.Generic;
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

using RVPDA.ViewModels;

namespace RVPDA.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ResolutionDialogue : Window
    {
        public int SelectedNumber { get; private set; }
        public ResolutionDialogue(int previousValue)
        {
            InitializeComponent();
            DisableCurrentSettingButton(previousValue);
            var viewModel = new ResolutionDialogueViewModel();
            viewModel.OnDialogClose += (value) =>
            {
                SelectedNumber = value;
                DialogResult = true; // Close the dialog
                Close();
            };
            DataContext = viewModel;
        }

        private void DisableCurrentSettingButton(int setting)
        {
            switch (setting)
            {
                case 0:
                    B0.IsEnabled = false;
                    break;
                case 1:
                    B1.IsEnabled = false;
                    break;
                case 2:
                    B2.IsEnabled = false;
                    break;
                case 3:
                    B3.IsEnabled = false;
                    break;
                case 4:
                    B4.IsEnabled = false;
                    break;
                case 5:
                    B5.IsEnabled = false;
                    break;

            }
        }

    }
}

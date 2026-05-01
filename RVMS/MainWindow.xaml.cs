using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RVMS.ViewModels;


namespace RVMS.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            // Set the DataContext to the MainViewModel
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            viewModel.ChromatogramPlot.Name = "ChromatogramPlot";
            viewModel.SpectrumPlot.Name = "SpectrumPlot";

            ChromatogramContainer.Children.Add(viewModel.ChromatogramPlot);
            SpectrumContainer.Children.Add(viewModel.SpectrumPlot);
            
            viewModel.ChromatogramPlot.PreviewKeyDown += WpfPlot_KeyDown;
            viewModel.SpectrumPlot.PreviewKeyDown += WpfPlot_KeyDown;
            viewModel.ChromatogramPlot.KeyDown += WpfPlot_CtrlKeyDown;
            viewModel.SpectrumViewModel.PropertyChanged += SpectrumViewModel_OnPropertyChanged;
            _spectrumViewModel = viewModel.SpectrumViewModel;
        }

        private SpectrumViewModel _spectrumViewModel;

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void SpectrumViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {

                case "Polarity":
                    Info_Polarity.Text = (sender as SpectrumViewModel).Polarity;
                    break;
                case "NumberOfScans":
                    Info_NumberOfScans.Text = (sender as SpectrumViewModel).NumberOfScans;
                    break;
            }
        }


        private async void SpectrumViewModel_LoadProgress()
        {

        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                // Move to a parent that can take focus
                FrameworkElement parent = (FrameworkElement)(textBox).Parent;
                while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
                {
                    parent = (FrameworkElement)parent.Parent;
                }

                DependencyObject scope = FocusManager.GetFocusScope(textBox);
                FocusManager.SetFocusedElement(scope, parent as IInputElement);
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));

            int massResolution = 0;

            if (this.DataContext is MainViewModel oldViewModel)
            {
                oldViewModel.ChromatogramPlot.PreviewKeyDown -= WpfPlot_KeyDown;
                oldViewModel.SpectrumPlot.PreviewKeyDown -= WpfPlot_KeyDown;
                oldViewModel.SpectrumViewModel.PropertyChanged -= SpectrumViewModel_OnPropertyChanged;
                massResolution = oldViewModel.MassResolutionDecimal;
                oldViewModel.UnsubscribeMainViewModel();
            }

            // Set the DataContext to the MainViewModel
            var viewModel = new MainViewModel();
            this.DataContext = viewModel;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            viewModel.ChromatogramPlot.Name = "ChromatogramPlot";
            viewModel.SpectrumPlot.Name = "SpectrumPlot";

            viewModel.MassResolutionDecimal = massResolution;

            ChromatogramContainer.Children.Clear();
            SpectrumContainer.Children.Clear();

            ChromatogramContainer.Children.Add(viewModel.ChromatogramPlot);
            SpectrumContainer.Children.Add(viewModel.SpectrumPlot);

            viewModel.ChromatogramPlot.PreviewKeyDown += WpfPlot_KeyDown;
            viewModel.SpectrumPlot.PreviewKeyDown += WpfPlot_KeyDown;
            viewModel.SpectrumViewModel.PropertyChanged += SpectrumViewModel_OnPropertyChanged;
            PlotSettings.Instance.ResetOnNewClick();
        }

        public void IncrementScanNumber(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;

            var x = (sender as Button);
            var option = x.Content as string;

            switch (option)
            {
                case "+ 10 Scans":
                    DC.IncrementScan(10);
                    break;
                case "+ 1 Scan":
                    DC.IncrementScan(1);
                    break;
                case "- 1 Scan":
                    DC.IncrementScan(-1);
                    break;
                case "- 10 Scans":
                    DC.IncrementScan(-10);
                    break;
            }
        }

        private void WpfPlot_KeyDown(object sender, KeyEventArgs e)
        {
            // Suppress arrow key interactions
            if (e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true; // Prevent default action
            }

            switch (e.Key)
            {
                case Key.Right:
                    (DataContext as MainViewModel).IncrementScan(1);
                    break;
                case Key.Left:
                    (DataContext as MainViewModel).IncrementScan(-1);
                    break;

            }
        }

        private void WpfPlot_CtrlKeyDown(object sender, KeyEventArgs e)
        {
            //
        }

        public void ChangeMassResolution(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;
            ResolutionDialogue dialog = new ResolutionDialogue(DC.MassResolutionDecimal);
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (dialog.ShowDialog() == true)
            {
                string filePath = DC.SelectedFilePath;
                New_Click(sender,e);
                DC = DataContext as MainViewModel;
                DC.MassResolutionDecimal = dialog.SelectedNumber;
                DC.SelectedFilePath = filePath;
                DC.MassResolutionChanged();
            }
        }

        public void ChangeMapScaling(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;
            ScalingDialogue dialog = new ScalingDialogue(PlotSettings.Instance.Chromatogram.MapScaling);

            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (dialog.ShowDialog() == true)
            {
                PlotSettings.Instance.Chromatogram.MapScaling = dialog.SelectedScalingMethod;
                DC.ScalingMethodChanged();
            }
        }

        public void ChangeColormap(object sender, RoutedEventArgs e)
        {
            var DC = DataContext as MainViewModel;
            ColormapSettingsDialogue dialog = new ColormapSettingsDialogue(DC.GetColormapSetting());
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            dialog.Left = this.Left - 300;
            dialog.Top = this.Top;

            if (dialog.ShowDialog() == true)
            {
                DC.SetColormapSetting(dialog.SelectColormapName);
                DC.ScalingMethodChanged();
            }
        }

        private void ChangeMassRange(object sender, RoutedEventArgs e)
        {
            LimitMassRangeView dialog = new LimitMassRangeView();
            dialog.Owner = Application.Current.MainWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (dialog.ShowDialog() == true)
            {

            }
        }

        public void About_Click(object sender, RoutedEventArgs e)
        {
            About aboutWindow = new About();
            aboutWindow.Owner = Application.Current.MainWindow;
            aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (aboutWindow.ShowDialog() == true)
            {
                //
            }
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            foreach (var window in Application.Current.Windows)
            {
                (window as Window).Close();
            }
        }
    }
}


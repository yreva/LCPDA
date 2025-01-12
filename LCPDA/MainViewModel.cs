using System.ComponentModel;
using System.Windows.Input;
using MyWpfApp.ViewModels;

namespace LCPDA.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ChromatogramViewModel _chromatogramViewModel;
        private SpectrumViewModel _spectrumViewModel;

        public ChromatogramViewModel ChromatogramViewModel
        {
            get => _chromatogramViewModel;
            set
            {
                _chromatogramViewModel = value;
                OnPropertyChanged(nameof(ChromatogramViewModel));
            }
        }

        public SpectrumViewModel SpectrumViewModel
        {
            get => _spectrumViewModel;
            set
            {
                _spectrumViewModel = value;
                OnPropertyChanged(nameof(SpectrumViewModel));
            }
        }

        public MainViewModel()
        {
            // Initialize the ViewModels for both plots
            ChromatogramViewModel = new ChromatogramViewModel();
            SpectrumViewModel = new SpectrumViewModel();
        }

        private int _currentScanNumber;
        public int CurrentScanNumber
        {
            get
            {
                return _currentScanNumber;
            }
            set
            {
                if (_currentScanNumber != value)
                {
                    _currentScanNumber = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }
}



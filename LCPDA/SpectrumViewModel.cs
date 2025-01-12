using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace LCPDA.ViewModels
{
    public class SpectrumViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Point> _dataPoints;

        public ObservableCollection<Point> DataPoints
        {
            get => _dataPoints;
            set
            {
                _dataPoints = value;
                OnPropertyChanged(nameof(DataPoints));
            }
        }

        public SpectrumViewModel()
        {
            // Initialize data for Plot 2
            DataPoints = new ObservableCollection<Point>
            {
                new Point(2, 3),
                new Point(4, 5),
                new Point(6, 7)
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

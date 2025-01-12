using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace MyWpfApp.ViewModels
{
    public class ChromatogramViewModel : INotifyPropertyChanged
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

        public ChromatogramViewModel()
        {
            // Initialize data for Plot 1
            DataPoints = new ObservableCollection<Point>
            {
                new Point(1, 2),
                new Point(3, 4),
                new Point(5, 6)
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RVMS.ViewModels
{
    internal class ResolutionDialogueViewModel
    {
        public ICommand SelectNumberCommand { get; }

        public event Action<int> OnDialogClose;

        public ResolutionDialogueViewModel()
        {
            SelectNumberCommand = new RelayCommand<object>(SelectNumber);
        }

        private void SelectNumber(object parameter)
        {
            if (parameter is string str && int.TryParse(str, out int selectedNumber))
            {
                OnDialogClose?.Invoke(selectedNumber);
            }
        }
    }
}

using MvvmGo.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SignalGoAddReferenceShared.ViewModels.LogicViewModels
{
    public class ManageSkipAssembliesViewModel : BaseViewModel
    {
        public ManageSkipAssembliesViewModel()
        {
            AddSkipAssemblyCommand = new Command(AddSkipAssembly);
            RemoveSkipAssemblyCommand = new Command<string>((value) =>
            {
                SkipAssemblies.Remove(value);
            });
        }

        public Command AddSkipAssemblyCommand { get; set; }
        public Command<string> RemoveSkipAssemblyCommand { get; set; }

        string _SkipAssemblyName;

        public string SkipAssemblyName
        {
            get => _SkipAssemblyName;
            set
            {
                _SkipAssemblyName = value;
                OnPropertyChanged(nameof(SkipAssemblyName));
                AddSkipAssemblyCommand?.ValidateCanExecute();
            }
        }

        public ObservableCollection<string> SkipAssemblies { get; set; } = new ObservableCollection<string>();

        public void AddSkipAssembly()
        {
            if (SkipAssemblies.Any(x => x == SkipAssemblyName))
            {
                MessageBox.Show($"{SkipAssemblyName} exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (string.IsNullOrEmpty(SkipAssemblyName))
            {
                MessageBox.Show($"from value cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!SkipAssemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Please set assembly extension as .dll like space.example.dll", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SkipAssemblies.Add(SkipAssemblyName);
            SkipAssemblyName = null;
        }
    }
}

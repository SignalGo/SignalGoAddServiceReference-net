using MvvmGo.Commands;
using SignalGo.Shared.Models.ServiceReference;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SignalGoAddReferenceShared.ViewModels.LogicViewModels
{
    public class ManageNamespacesViewModel : BaseViewModel
    {
        public ManageNamespacesViewModel()
        {
            AddNameSpaceCommand = new Command(AddNameSpace);
            RmoveNameSpaceCommand = new  Command<ReplaceNameSpaceInfo>(RemoveNameSpace);
        }

        public Command AddNameSpaceCommand { get; set; }
        public Command<ReplaceNameSpaceInfo> RmoveNameSpaceCommand { get; set; }
        string _CustomNamespaces;
        bool _IsGlobal;
        string _FromNamespace;
        string _ToNamespace;
        public string CustomNamespaces
        {
            get => _CustomNamespaces;
            set
            {
                _CustomNamespaces = value;
                OnPropertyChanged(nameof(CustomNamespaces));
            }
        }

        public bool IsGlobal
        {
            get => _IsGlobal;
            set
            {
                _IsGlobal = value;
                OnPropertyChanged(nameof(IsGlobal));
            }
        }

        public string FromNamespace
        {
            get => _FromNamespace;
            set
            {
                _FromNamespace = value;
                OnPropertyChanged(nameof(FromNamespace));
            }
        }

        public string ToNamespace
        {
            get => _ToNamespace;
            set
            {
                _ToNamespace = value;
                OnPropertyChanged(nameof(ToNamespace));
            }
        }

        public ObservableCollection<ReplaceNameSpaceInfo> ReplaceNameSpaces { get; set; } = new ObservableCollection<ReplaceNameSpaceInfo>();

        private void AddNameSpace()
        {
            if (ReplaceNameSpaces.Any(x => x.From == FromNamespace))
            {
                MessageBox.Show($"{FromNamespace} exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(FromNamespace) && !IsGlobal)
            {
                MessageBox.Show($"from value cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!string.IsNullOrEmpty(FromNamespace) && IsGlobal)
            {
                MessageBox.Show($"you cannot set text for global reference replacement, please empty from textbox then try again or uncheck it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(FromNamespace) && IsGlobal && ReplaceNameSpaces.Any(x => x.IsGlobal))
            {
                MessageBox.Show($"you cannot global replacement double time", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                ReplaceNameSpaces.Add(new ReplaceNameSpaceInfo() { From = FromNamespace, To = ToNamespace, IsGlobal = IsGlobal });
                FromNamespace = "";
                ToNamespace = "";
            }
        }

        private void RemoveNameSpace(ReplaceNameSpaceInfo  replaceNameSpace)
        {
            ReplaceNameSpaces.Remove(replaceNameSpace);
        }
    }
}

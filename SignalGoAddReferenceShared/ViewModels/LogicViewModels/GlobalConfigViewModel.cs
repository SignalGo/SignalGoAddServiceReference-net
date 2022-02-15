using System;

namespace SignalGoAddReferenceShared.ViewModels.LogicViewModels
{
    public class GlobalConfigViewModel : BaseViewModel
    {
        string _ServiceAddress;
        string _ServiceDefaultNamespace;

        public string ServiceAddress
        {
            get => _ServiceAddress;
            set
            {
                _ServiceAddress = value;
                OnPropertyChanged(nameof(ServiceAddress), () => Uri.TryCreate(_ServiceAddress, UriKind.Absolute, out _));
            }
        }

        public string ServiceDefaultNamespace
        {
            get => _ServiceDefaultNamespace;
            set
            {
                _ServiceDefaultNamespace = value;
                OnPropertyChanged(nameof(ServiceDefaultNamespace), () => !string.IsNullOrEmpty(_ServiceDefaultNamespace));
            }
        }
    }

}

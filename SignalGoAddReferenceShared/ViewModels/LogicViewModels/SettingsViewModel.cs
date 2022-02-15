namespace SignalGoAddReferenceShared.ViewModels.LogicViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        int _SelectedLanguageType = 0;
        int _SelectedServiceType = 0;
        bool _IsJustServices = false;
        bool _IsGenerateAsyncMethods = true;
        bool _IsAutomaticSyncAndAsyncDetection = true;
        public int SelectedLanguageType
        {
            get => _SelectedLanguageType;
            set
            {
                _SelectedLanguageType = value;
                OnPropertyChanged(nameof(SelectedLanguageType));
            }
        }

        public int SelectedServiceType
        {
            get => _SelectedServiceType;
            set
            {
                _SelectedServiceType = value;
                OnPropertyChanged(nameof(SelectedServiceType));
            }
        }

        public bool IsJustServices
        {
            get => _IsJustServices;
            set
            {
                _IsJustServices = value;
                OnPropertyChanged(nameof(IsJustServices));
            }
        }

        public bool IsGenerateAsyncMethods
        {
            get => _IsGenerateAsyncMethods;
            set
            {
                _IsGenerateAsyncMethods = value;
                OnPropertyChanged(nameof(IsGenerateAsyncMethods));
            }
        }

        public bool IsAutomaticSyncAndAsyncDetection
        {
            get => _IsAutomaticSyncAndAsyncDetection;
            set
            {
                _IsAutomaticSyncAndAsyncDetection = value;
                OnPropertyChanged(nameof(IsAutomaticSyncAndAsyncDetection));
            }
        }
    }
}

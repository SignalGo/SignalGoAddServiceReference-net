using Microsoft.VisualStudio.ConnectedServices;
using SignalGoAddReferenceShared.ViewModels.LogicViewModels;
using SignalGoAddReferenceShared.Views;

namespace SignalGoAddReferenceShared.ViewModels.WizardViewModels
{
    public class SettingsWizardViewModel : ConnectedServiceWizardPage
    {
        public ConnectedServiceWizard Wizard;
        public static SettingsViewModel ViewModel { get; set; }
        public SettingsWizardViewModel(ConnectedServiceWizard wizard)
        {
            this.Legend = "Settings";

            Wizard = wizard;
            this.View = new SettingsView();

            ViewModel = View.DataContext as SettingsViewModel;
            GlobalConfigWizardViewModel.ViewModel.Changed = (isValidate) =>
            {
                IsEnabled = isValidate;
                ViewModel.IsValidate = isValidate;
            };
        }
    }
}
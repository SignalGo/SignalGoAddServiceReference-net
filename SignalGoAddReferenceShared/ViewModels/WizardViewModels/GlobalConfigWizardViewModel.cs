using Microsoft.VisualStudio.ConnectedServices;
using SignalGoAddReferenceShared.ViewModels.LogicViewModels;
using SignalGoAddReferenceShared.Views;

namespace SignalGoAddReferenceShared.ViewModels.WizardViewModels
{
    public class GlobalConfigWizardViewModel : ConnectedServiceWizardPage
    {
        public ConnectedServiceWizard Wizard;
        public static GlobalConfigViewModel ViewModel { get; set; }
        public GlobalConfigWizardViewModel(ConnectedServiceWizard wizard)
        {
            this.Legend = "Global Config";
            Wizard = wizard;
            View = new GlobalConfigView();
            ViewModel = View.DataContext as GlobalConfigViewModel;
        }
    }
}

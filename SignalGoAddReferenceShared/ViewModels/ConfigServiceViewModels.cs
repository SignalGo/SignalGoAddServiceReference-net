using Microsoft.VisualStudio.ConnectedServices;
using SignalGoAddReferenceShared.Views;
using SignalGoAddServiceReferenceCore;

namespace SignalGoAddReferenceShared.ViewModels
{
    internal class ConfigServiceViewModels : ConnectedServiceWizardPage
    {
        public Wizard Wizard;
        public ConfigServiceViewModels(Wizard wizard)
        {
            Wizard = wizard;
            this.View = new AddOrUpdateServiceConfig() { DataContext = this };
        }
    }
}

using Microsoft.VisualStudio.ConnectedServices;
using SignalGoAddReferenceShared.ViewModels.LogicViewModels;
using SignalGoAddReferenceShared.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace SignalGoAddReferenceShared.ViewModels.WizardViewModels
{
    public class ManageNamespacesWizardViewModel : ConnectedServiceWizardPage
    {
        public ConnectedServiceWizard Wizard;
        public static ManageNamespacesViewModel ViewModel;
        public ManageNamespacesWizardViewModel(ConnectedServiceWizard wizard)
        {
            this.Legend = "Manage Namespaces";
            Wizard = wizard;
            this.View = new ManageNamespacesView();
            ViewModel = View.DataContext as ManageNamespacesViewModel;

            SettingsWizardViewModel.ViewModel.Changed = (isValidate) =>
            {
                IsEnabled = isValidate;
                ViewModel.IsValidate = isValidate;
            };
        }

    }
}

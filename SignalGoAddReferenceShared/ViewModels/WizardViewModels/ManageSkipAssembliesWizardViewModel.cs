using Microsoft.VisualStudio.ConnectedServices;
using SignalGoAddReferenceShared.ViewModels.LogicViewModels;
using SignalGoAddReferenceShared.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace SignalGoAddReferenceShared.ViewModels.WizardViewModels
{
    public class ManageSkipAssembliesWizardViewModel : ConnectedServiceWizardPage
    {
        public ConnectedServiceWizard Wizard;
        public static ManageSkipAssembliesViewModel ViewModel { get; set; }
        public ManageSkipAssembliesWizardViewModel(ConnectedServiceWizard wizard)
        {
            //this.Title = "Configure specification endpoint";
            //this.Description = "Enter or choose an specification endpoint and check generation options to begin";
            this.Legend = "Manage Skip Assemblies";
            Wizard = wizard;
            this.View = new ManageSkipAssembliesView();
            ViewModel = View.DataContext as ManageSkipAssembliesViewModel;
            ManageNamespacesWizardViewModel.ViewModel.Changed = (isValidate) =>
            {
                IsEnabled = isValidate;
                ViewModel.IsValidate = isValidate;
            };
            GlobalConfigWizardViewModel.ViewModel.CheckValidations();
        }
    }
}

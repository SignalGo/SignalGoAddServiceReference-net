using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ConnectedServices;
using SignalGoAddReferenceShared.ViewModels;
using SignalGoAddReferenceShared.ViewModels.WizardViewModels;
using SignalGoAddReferenceShared.Views;

namespace SignalGoAddServiceReferenceCore
{
    internal class Wizard : ConnectedServiceWizard
    {
        internal readonly string ProjectPath;

        public ConnectedServiceProviderContext Context { get; set; }

        public Wizard(ConnectedServiceProviderContext context)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            this.Context = context;
            //this.ProjectPath = context.ProjectHierarchy?.GetProject().Properties.Item("FullPath").Value.ToString();

            if (this.Context.IsUpdating)
            {

            }

            this.Pages.Add(new GlobalConfigWizardViewModel(this));
            this.Pages.Add(new SettingsWizardViewModel(this));
            this.Pages.Add(new ManageNamespacesWizardViewModel(this));
            this.Pages.Add(new ManageSkipAssembliesWizardViewModel(this));
            this.IsFinishEnabled = true;
        }


        public override async Task<ConnectedServiceInstance> GetFinishedServiceInstanceAsync()
        {
            return new Instance();
        }
    }
}

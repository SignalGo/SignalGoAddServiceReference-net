using EnvDTE;
using Microsoft.VisualStudio.ConnectedServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using SignalGo.CodeGenerator.Models;
using SignalGoAddReferenceShared.Helpers;
using SignalGoAddReferenceShared.Models;
using SignalGoAddReferenceShared.ViewModels.WizardViewModels;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalGoAddServiceReferenceCore
{
    [ConnectedServiceHandlerExport(Constants.ProviderId, AppliesTo = "VB | CSharp | Web")]
    internal class Handler : ConnectedServiceHandler
    {
        #region Methods

        //public override async Task<AddServiceInstanceResult> AddServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken cancellationToken)
        //{
        //    var instance = (Instance)context.ServiceInstance;
        //    await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, $"Adding service instance for \"{instance.ServiceConfig.Endpoint}\"...");

        //    // await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Checking prerequisites...");
        //    // await CheckingPrerequisitesAsync(context, instance);

        //    var codeGenDescriptor = await GenerateCodeAsync(context, instance);
        //    context.SetExtendedDesignerData(instance.ServiceConfig);
        //    await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Adding service instance complete!");
        //    return new AddServiceInstanceResult(context.ServiceInstance.Name, new Uri(Constants.Website));
        //}

        //public override async Task<UpdateServiceInstanceResult> UpdateServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken cancellationToken)
        //{
        //    var instance = (Instance)context.ServiceInstance;
        //    await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, $"Re-adding service instance for \"{instance.ServiceConfig.Endpoint}\"...");

        //    // await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Checking prerequisites...");
        //    // await CheckingPrerequisitesAsync(context, instance);

        //    var codeGenDescriptor = await ReGenerateCodeAsync(context, instance);
        //    context.SetExtendedDesignerData(instance.ServiceConfig);
        //    await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Re-Adding service instance complete!");
        //    return await base.UpdateServiceInstanceAsync(context, cancellationToken);
        //}

        //private static async Task<BaseCodeGenDescriptor> GenerateCodeAsync(ConnectedServiceHandlerContext context, Instance instance)
        //{
        //    var codeGenDescriptor = new NSwagCodeGenDescriptor(context, instance);
        //    await codeGenDescriptor.AddNugetPackagesAsync();
        //    var nswagFilePath = await codeGenDescriptor.AddGeneratedNswagFileAsync();
        //    var clientFilePath = await codeGenDescriptor.AddGeneratedCodeAsync();
        //    return codeGenDescriptor;
        //}

        //private static async Task<BaseCodeGenDescriptor> ReGenerateCodeAsync(ConnectedServiceHandlerContext context, Instance instance)
        //{
        //    var codeGenDescriptor = new NSwagCodeGenDescriptor(context, instance);
        //    var nswagFilePath = await codeGenDescriptor.AddGeneratedNswagFileAsync();
        //    var clientFilePath = await codeGenDescriptor.AddGeneratedCodeAsync();
        //    return codeGenDescriptor;
        //}

        #endregion
        public override async Task<AddServiceInstanceResult> AddServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken ct)
        {
            await new JoinableTaskFactory(ThreadHelper.JoinableTaskContext).SwitchToMainThreadAsync(ct);
            Project project = ((ProjectInfo)LanguageMap.Current.GetActiveProject()).Project;
            string projectPath = project.FullName;
            string servicesFolder = Path.Combine(Path.GetDirectoryName(projectPath), "Connected Services");
            if (!Directory.Exists(servicesFolder))
                project.ProjectItems.AddFolder("Connected Services");
            Uri uri = null;
            string serviceNameSpace = GlobalConfigWizardViewModel.ViewModel.ServiceDefaultNamespace.Trim();
            string serviceURI = GlobalConfigWizardViewModel.ViewModel.ServiceAddress.Trim();
            if (string.IsNullOrEmpty(serviceNameSpace))
            {
                throw new Exception("Please fill your service name");
            }
            else if (!Uri.TryCreate(serviceURI, UriKind.Absolute, out uri))
            {
                throw new Exception("Service address is not true");
            }

            string servicePath = Path.Combine(servicesFolder, Path.GetFileNameWithoutExtension(serviceNameSpace));
            if (!Directory.Exists(servicePath))
                Directory.CreateDirectory(servicePath);
            AddReferenceConfigInfo config = new AddReferenceConfigInfo
            {
                ServiceUrl = serviceURI,
                ServiceNameSpace = serviceNameSpace,
                LanguageType = SettingsWizardViewModel.ViewModel.SelectedLanguageType,
                ServiceType = SettingsWizardViewModel.ViewModel.SelectedServiceType,
                IsGenerateAsyncMethods = SettingsWizardViewModel.ViewModel.IsGenerateAsyncMethods,
                IsJustGenerateServices = SettingsWizardViewModel.ViewModel.IsJustServices,
                IsAutomaticSyncAndAsyncDetection = SettingsWizardViewModel.ViewModel.IsAutomaticSyncAndAsyncDetection,
                ReplaceNameSpaces = ManageNamespacesWizardViewModel.ViewModel.ReplaceNameSpaces.ToList(),
                SkipAssemblies = ManageSkipAssembliesWizardViewModel.ViewModel.SkipAssemblies.ToList(),
                CustomNameSpaces = ManageNamespacesWizardViewModel.ViewModel.CustomNamespaces
            };

            await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, $"Configuration passed");
            await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, $"Collecting data from {serviceURI}");
            LanguageMap.Current.Context = context;
            string fullFilePath = await LanguageMap.Current.DownloadService(servicePath, config);

            string signalGoSettingPath = Path.Combine(servicePath, "setting.signalgo");
            File.WriteAllText(signalGoSettingPath, JsonConvert.SerializeObject(config), Encoding.UTF8);

            if (!string.IsNullOrEmpty(fullFilePath))
                project.ProjectItems.AddFromFile(fullFilePath);

            await context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, $"Service {serviceNameSpace} generated successfully");
            return new AddServiceInstanceResult(Path.GetFileName(servicePath), null);
        }
    }
}

﻿using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using SignalGo.CodeGenerator.Models;
using SignalGo.Shared.Models.ServiceReference;
using SignalGoAddReferenceShared.Helpers;
using SignalGoAddReferenceShared.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace SignalGoAddReferenceShared.Views
{
    public sealed partial class AddOrUpdateServiceConfig : UserControl
    {
        public AddOrUpdateServiceConfig()
        {
            this.InitializeComponent();
            lstReplaceNameSpaces.ItemsSource = ReplaceNameSpaces;
            lstSkipAssemblies.ItemsSource = SkipAssemblies;
        }

        public static object GetSelectedItem()
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint itemId;

            IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

            try
            {
                monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out itemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);

                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                     hierarchyPointer,
                                                     typeof(IVsHierarchy)) as IVsHierarchy;

                if (selectedHierarchy != null)
                {
                    ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
                }

                Marshal.Release(hierarchyPointer);
                Marshal.Release(selectionContainerPointer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }

            return selectedObject;
        }

        public Action FinishedAction { get; set; }

        public ObservableCollection<ReplaceNameSpaceInfo> ReplaceNameSpaces { get; set; } = new ObservableCollection<ReplaceNameSpaceInfo>();
        public ObservableCollection<string> SkipAssemblies { get; set; } = new ObservableCollection<string>();
        private async void btnAddService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnAddService.IsEnabled = false;
                Project project = ((ProjectInfo)LanguageMap.Current.GetActiveProject()).Project;
                string projectPath = project.FullName;
                string servicesFolder = Path.Combine(Path.GetDirectoryName(projectPath), "Connected Services");
                if (!Directory.Exists(servicesFolder))
                    project.ProjectItems.AddFolder("Connected Services");
                Uri uri = null;
                string serviceNameSpace = txtServiceName.Text.Trim();
                string serviceURI = txtServiceAddress.Text.Trim();
                if (string.IsNullOrEmpty(serviceNameSpace))
                {
                    MessageBox.Show("Please fill your service name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (!Uri.TryCreate(serviceURI, UriKind.Absolute, out uri))
                {
                    MessageBox.Show("Service address is not true", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string servicePath = Path.Combine(servicesFolder, Path.GetFileNameWithoutExtension(serviceNameSpace));
                if (!Directory.Exists(servicePath))
                    Directory.CreateDirectory(servicePath);
                AddReferenceConfigInfo config = new AddReferenceConfigInfo
                {
                    ServiceUrl = serviceURI,
                    ServiceNameSpace = serviceNameSpace,
                    LanguageType = cboLanguage.SelectedIndex,
                    ServiceType = cboServiceType.SelectedIndex,
                    IsGenerateAsyncMethods = chkAsyncMethods.IsChecked.Value,
                    IsJustGenerateServices = chkJustServices.IsChecked.Value,
                    IsAutomaticSyncAndAsyncDetection = rdoIsAutomaticDetection.IsChecked.Value,
                    ReplaceNameSpaces = ReplaceNameSpaces.ToList(),
                    SkipAssemblies = SkipAssemblies.ToList(),
                    CustomNameSpaces = customNameSpaces.Text
                };

                string fullFilePath = await LanguageMap.Current.DownloadService(servicePath, config);

                string signalGoSettingPath = Path.Combine(servicePath, "setting.signalgo");
                File.WriteAllText(signalGoSettingPath, JsonConvert.SerializeObject(config), Encoding.UTF8);

                if (!string.IsNullOrEmpty(fullFilePath))
                    project.ProjectItems.AddFromFile(fullFilePath);
                FinishedAction?.Invoke();
                MessageBox.Show($"Service {serviceNameSpace} generated", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnAddService.IsEnabled = true;
            }
        }

        private void BtnRemoveReplaceNameSpaces_Click(object sender, RoutedEventArgs e)
        {
            ReplaceNameSpaces.Remove((ReplaceNameSpaceInfo)((Button)sender).DataContext);
        }

        private void BtnAddNameSpace_Click(object sender, RoutedEventArgs e)
        {
            if (ReplaceNameSpaces.Any(x => x.From == fromNameSpace.Text))
            {
                MessageBox.Show($"{fromNameSpace.Text} exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(fromNameSpace.Text) && !chkIsGlobal.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show($"from value cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!string.IsNullOrEmpty(fromNameSpace.Text) && chkIsGlobal.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show($"you cannot set text for global reference replacement, please empty from textbox then try again or uncheck it", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(fromNameSpace.Text) && chkIsGlobal.IsChecked.GetValueOrDefault() && ReplaceNameSpaces.Any(x => x.IsGlobal))
            {
                MessageBox.Show($"you cannot global replacement double time", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                ReplaceNameSpaces.Add(new ReplaceNameSpaceInfo() { From = fromNameSpace.Text, To = toNameSpace.Text, IsGlobal = chkIsGlobal.IsChecked.GetValueOrDefault() });
                fromNameSpace.Text = "";
                toNameSpace.Text = "";
            }
        }

        private void btnAddSkipAssembly_Click(object sender, RoutedEventArgs e)
        {
            if (SkipAssemblies.Any(x => x == txtSkipAssembly.Text))
            {
                MessageBox.Show($"{txtSkipAssembly.Text} exist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (string.IsNullOrEmpty(txtSkipAssembly.Text))
            {
                MessageBox.Show($"from value cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (!txtSkipAssembly.Text.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Please set assembly extension as .dll like space.example.dll", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SkipAssemblies.Add(txtSkipAssembly.Text);
            txtSkipAssembly.Text = "";
        }

        private void btnSkipAssembly_Click(object sender, RoutedEventArgs e)
        {
            SkipAssemblies.Remove((string)((Button)sender).DataContext);
        }
    }
}

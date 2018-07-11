namespace SignalGoAddServiceReference
{
    using EnvDTE;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Linq;
    using Newtonsoft.Json;
    using SignalGo.Shared.Models.ServiceReference;
    using System.Reflection;
    using SignalGoAddServiceReference.Models;
    using System.Text.RegularExpressions;
    using SignalGoAddServiceReference.LanguageMaps;

    /// <summary>
    /// Interaction logic for AddServiceWindowControl.
    /// </summary>
    public partial class AddServiceWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddServiceWindowControl"/> class.
        /// </summary>
        public AddServiceWindowControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        //[SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        //[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        //private void button1_Click(object sender, RoutedEventArgs e)
        //{
        //    MessageBox.Show(
        //        string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
        //        "AddServiceWindow");
        //}
        public static object GetSelectedItem()
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint itemId;

            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

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
        private void btnAddService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = BaseLanguageMap.GetActiveProject();
                var projectPath = project.FullName;
                string servicesFolder = Path.Combine(Path.GetDirectoryName(projectPath), "Connected Services");
                if (!Directory.Exists(servicesFolder))
                    project.ProjectItems.AddFolder("Connected Services");
                Uri uri = null;
                var serviceNameSpace = txtServiceName.Text.Trim();
                var serviceURI = txtServiceAddress.Text.Trim();
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
                var fullFilePath = BaseLanguageMap.DownloadService(uri, servicePath, serviceNameSpace, cboLanguage.SelectedIndex);

                StringBuilder text = new StringBuilder();
                text.AppendLine(serviceURI);
                text.AppendLine(serviceNameSpace);
                text.AppendLine(cboLanguage.SelectedIndex.ToString());
                var signalGoSettingPath = Path.Combine(servicePath, "setting.signalgo");
                File.WriteAllText(signalGoSettingPath, text.ToString(), Encoding.UTF8);

                project.ProjectItems.AddFromFile(fullFilePath);
                FinishedAction?.Invoke();
                MessageBox.Show($"Service {serviceNameSpace} created", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
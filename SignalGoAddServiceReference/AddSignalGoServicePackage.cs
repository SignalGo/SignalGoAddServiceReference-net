using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ExtensionManager;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace SignalGoAddServiceReference
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(AddServiceWindow))]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.CSharpProject_string)]
    public sealed class AddSignalGoServicePackage : Package
    {
        /// <summary>
        /// AddSignalGoServicePackage GUID string.
        /// </summary>
        public const string PackageGuidString = "94784b9e-7818-4e0c-943e-d380824c4271";
        public static string Version { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AddSignalGoService"/> class.
        /// </summary>
        public AddSignalGoServicePackage()
        {
            try
            {
                // get ExtensionManager
                var manager = Package.GetGlobalService(typeof(SVsExtensionManager));
                // get your extension by Product Id
                var myExtension = manager.GetType().GetMethod("GetInstalledExtension").Invoke(manager,new object[] { "SignalGoExtension.0bde2334-d8c9-4dc4-851f-61016c295347" });
                // get current version
                var header = myExtension.GetType().GetProperty("Header").GetValue(myExtension, null);
                var currentVersion = header.GetType().GetProperty("Version").GetValue(header, null);
                Version = currentVersion.ToString();
            }
            catch (Exception ex)
            {
                Version = "unknown";
            }
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            AddSignalGoService.Initialize(this);
            base.Initialize();
            AddServiceWindowCommand.Initialize(this);
            UpdateSignalGoServiceCommand.Initialize(this);
            ConfigSignalGoServiceCommand.Initialize(this);

        }

        #endregion
    }

}

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using SignalGo.CodeGenerator.Helpers;
using SignalGo.CodeGenerator.Models;
using SignalGoAddReferenceShared.Helpers;

namespace SignalGoAddServiceReferenceCore.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class UpdateSignalGoServiceCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("2665a80a-ad4e-4fc1-b426-5fc503b88fa1");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSignalGoServiceCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private UpdateSignalGoServiceCommand(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }
            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                System.ComponentModel.Design.CommandID dynVisibiliytyCmdID = new CommandID(CommandSet, CommandId);
                OleMenuCommand dynamicVisibiltyCmdMenuCommand = new OleMenuCommand(new EventHandler(this.MenuItemCallback), dynVisibiliytyCmdID);
                commandService.AddCommand(dynamicVisibiltyCmdMenuCommand);
                dynamicVisibiltyCmdMenuCommand.BeforeQueryStatus += new EventHandler(MenuItem_BeforeQueryStatus);
            }
        }

        private void MenuItem_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = sender as OleMenuCommand;
            try
            {
                EnvDTE.DTE dte = Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;

                var projectItem = dte.SelectedItems.Item(1).ProjectItem;
                var directory = Path.GetDirectoryName(projectItem.FileNames[0]);
                var settingFile = Path.Combine(directory, "setting.signalgo");
                if (File.Exists(settingFile))
                    command.Visible = true;
                else
                    command.Visible = false;
            }
            catch (Exception)
            {
                command.Visible = false;
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static UpdateSignalGoServiceCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new UpdateSignalGoServiceCommand(package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                EnvDTE.DTE dte = Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;

                var projectItem = dte.SelectedItems.Item(1).ProjectItem;
                var directory = Path.GetDirectoryName(projectItem.FileNames[0]);
                var settingFile = Path.Combine(directory, "setting.signalgo");
                if (File.Exists(settingFile))
                {
                    AddReferenceConfigInfo config = null;
                    try
                    {
                        config = JsonConvert.DeserializeObject<AddReferenceConfigInfo>(File.ReadAllText(settingFile, Encoding.UTF8));
                    }
                    catch (Exception ex)
                    {
                        config = new AddReferenceConfigInfo();
                        var lines = File.ReadAllLines(settingFile, Encoding.UTF8);
                        if (lines.Length <= 1)
                        {
                            MessageBox.Show("Setting file is empty! please try to recreate your service!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        config.ServiceUrl = lines[0];
                        config.ServiceNameSpace = lines[1];
                        config.LanguageType = int.Parse(lines[2]);
                    }


                    if (Uri.TryCreate(config.ServiceUrl, UriKind.Absolute, out Uri uri))
                    {
                        new JoinableTaskFactory(ThreadHelper.JoinableTaskContext).Run(async () => await LanguageMap.Current.DownloadService(directory, config));
                        MessageBox.Show("Update success!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                        MessageBox.Show("Service address is not true", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                    MessageBox.Show("setting file not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

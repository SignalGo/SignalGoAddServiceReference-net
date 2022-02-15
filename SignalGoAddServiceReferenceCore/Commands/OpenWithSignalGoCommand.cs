using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace SignalGoAddServiceReferenceCore.Commands
{
    internal sealed class OpenWithSignalGoCommand
    {
        #region Properties and fields

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int OpenInNSwagStudioCommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid OpenInNSwagStudioCommandSet = new Guid("ac8fd210-0b54-49cc-8151-7f5c2ecbf733");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage _package;

        /// <summary>
        /// <see cref="DTE2"/>.
        /// </summary>
        private readonly DTE2 _dte;

        #endregion

        #region Constructors

        private OpenWithSignalGoCommand(AsyncPackage package, OleMenuCommandService commandService, DTE2 dte)
        {
            this._package = package ?? throw new ArgumentNullException(nameof(package));
            _dte = dte;
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            var menuCommandId = new CommandID(OpenInNSwagStudioCommandSet, OpenInNSwagStudioCommandId);
            var menuItem = new OleMenuCommand(ShowToolWindow, menuCommandId);
            menuItem.BeforeQueryStatus += BeforeQueryStatusCallback;
            commandService.AddCommand(menuItem);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static OpenWithSignalGoCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => this._package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="options">Options.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in OpenWithNSwagStudioCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            var dte = await package.GetServiceAsync(typeof(DTE)) as DTE2;
            Instance = new OpenWithSignalGoCommand(package, commandService, dte);
        }

        /// <summary>
        /// This function is the callback used for <see cref="OleMenuCommand.BeforeQueryStatus"/>.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void BeforeQueryStatusCallback(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            //var path = ProjectHelper.GetSelectedPath(_dte);
            cmd.Visible = true;// !string.IsNullOrWhiteSpace(path) && !Directory.Exists(path.Trim('"')) && (path.Trim('"').EndsWith(".nswag") || path.Trim('"').EndsWith(".nswag.json"));
            cmd.Enabled = cmd.Visible;
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {

        }
        #endregion
    }
}

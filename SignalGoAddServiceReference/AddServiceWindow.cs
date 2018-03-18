namespace SignalGoAddServiceReference
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("39924018-8cf0-4071-8490-c2b16fbe17e9")]
    public class AddServiceWindow : DialogWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddServiceWindow"/> class.
        /// </summary>
        public AddServiceWindow() : base()
        {
            this.Title = "Add SignalGo Service Reference";
            this.SizeToContent =  System.Windows.SizeToContent.Height;
            this.Width = 500;
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new AddServiceWindowControl()
            {
                FinishedAction = () =>
                {
                    Close();
                }
            };
        }
    }
}

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.IO;
using System.Reflection;

namespace AdsUtilitiesVsIntegration
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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AdsUtilitiesVsIntegrationPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class AdsUtilitiesVsIntegrationPackage : AsyncPackage
    {
        /// <summary>
        /// AdsUtilitiesVsIntegrationPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "b87481e4-7568-4381-8902-25775e7bc58e";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // Button-Command initialisieren
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            CommandID menuCommandID = new CommandID(new Guid("5061690a-b002-407f-ba09-afee52d551a7"), 0x0100);
            MenuCommand menuItem = new MenuCommand(OpenWpfApp, menuCommandID);
            commandService?.AddCommand(menuItem);
        }

        private void OpenWpfApp(object sender, EventArgs e)
        {
            // ToDo: Pfad zur WPF-Anwendung anpassen 
            string extensionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string wpfAppPath = Path.Combine(extensionDir, "WpfUiFiles/AdsUtilitiesUI.exe");

            try
            {
                Process.Start(wpfAppPath);
            }
            catch (Exception ex)
            {
                // Fehlerbehandlung: Ausgabe im Visual Studio-Ausgabefenster oder in einer MessageBox
                VsShellUtilities.ShowMessageBox(
                    this,
                    $"Error starting WPF: {ex.Message}",
                    "Error",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        #endregion
    }
}

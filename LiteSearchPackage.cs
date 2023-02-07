using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using System.ComponentModel;
using System.Globalization;

namespace LiteSearch
{
    public sealed class OptionsAccessor
    {
        private static readonly OptionsAccessor instance = new OptionsAccessor();

        private OptionsAccessor()
        {
        }

        public static OptionsAccessor Instance
        {
            get
            {
                return instance;
            }
        }

        private int extraLines = 2;
        public int ExtraLines
        {
            get { return extraLines; }
            set 
            {
                if (value < 0)
                    extraLines = 0;
                else if (value > 10)
                    extraLines = 10;
                else
                    extraLines = value;
            }
        }

        private bool caseSensitiveSearch = true;

        [Category("Search options")]
        [DisplayName("Case Sensitive")]
        [Description("Case sensitivity setting")]
        public bool CaseSensitive
        {
            get { return caseSensitiveSearch; }
            set { caseSensitiveSearch = value; }
        }
    }

    public class OptionPageGrid : DialogPage
    {
        [Category("Viewer options")]
        [DisplayName("Extra lines")]
        [Description("Number of extra context lines around the searched text (Max = 10).")]
        public int ExtraLines
        {
            get { return OptionsAccessor.Instance.ExtraLines; }
            set { OptionsAccessor.Instance.ExtraLines = value; }
        }

        [Category("Search options")]
        [DisplayName("Case Sensitive")]
        [Description("Case sensitivity setting")]
        public bool CaseSensitive
        {
            get { return OptionsAccessor.Instance.CaseSensitive; }
            set { OptionsAccessor.Instance.CaseSensitive = value; }
        }
    }

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
    [Guid(LiteSearchPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionPageGrid),
    "LiteSearch", "General", 0, 0, true)]
    public sealed class LiteSearchPackage : AsyncPackage
    {
        /// <summary>
        /// LiteSearchPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "22101a20-2fa9-407d-89f5-f36e04a493a1";

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
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await Command.InitializeAsync(this);
        }

        #endregion

        public int OptionExtraLines
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.ExtraLines;
            }
        }

        public bool OptionCaseSensitive
        {
            get
            {
                OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
                return page.CaseSensitive;
            }
        }

    }
}

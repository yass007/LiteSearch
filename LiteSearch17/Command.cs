using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace LiteSearch
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("e855bfbb-be87-4335-beff-c580ada2e75d");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
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
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            //string message = "Say Hello World!";
            //string title = "Command";

            ////// Show a message box to prove we were here
            ////VsShellUtilities.ShowMessageBox(
            ////    this.package,
            ////    message,
            ////    title,
            ////    OLEMSGICON.OLEMSGICON_INFO,
            ////    OLEMSGBUTTON.OLEMSGBUTTON_OK,
            ////    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
            IVsTextView activeView = null;
            ErrorHandler.ThrowOnFailure(textManager.GetActiveView(1, null, out activeView));

            var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            var textView = editorAdapter.GetWpfTextView(activeView);

            var textBuffer = textView.TextBuffer;

            var tagger = textBuffer.Properties.GetProperty(typeof(ITagger<IOutliningRegionTag>)) as LSOutliningTagger;

            if(!tagger.AreTagsActive())
            {
                var textStructureNavigatorSelectorService = componentModel.GetService<ITextStructureNavigatorSelectorService>();

                var navigator = textStructureNavigatorSelectorService.GetTextStructureNavigator(textView.TextBuffer);

                SnapshotPoint activePoint;
                if (!textView.Selection.IsEmpty)
                    activePoint = textView.Selection.Start.Position;
                else
                    activePoint = textView.Caret.Position.BufferPosition;

                var currentWord = navigator.GetExtentOfWord(activePoint);
                if (!currentWord.Span.IsEmpty && currentWord.IsSignificant)
                {
                    string wordText = textView.TextSnapshot.GetText(currentWord.Span);
                    System.Diagnostics.Debug.WriteLine(wordText);

                    tagger.GenerateTags(wordText);

                    IOutliningManagerService outliningManagerService = componentModel.GetService<IOutliningManagerService>();

                    IOutliningManager _outliningManager = outliningManagerService.GetOutliningManager(textView);
                    var snapshot = textView.TextSnapshot;
                    var snapshotSpan = new Microsoft.VisualStudio.Text.SnapshotSpan(snapshot, new Microsoft.VisualStudio.Text.Span(0, snapshot.Length));

                    var regions = _outliningManager.GetAllRegions(snapshotSpan);
                    foreach (var reg in regions)
                    {
                        if (tagger.IsValidRegion(reg))
                        {
                            _outliningManager.TryCollapse(reg);
                        }
                    }
                }
            }
            else
            {
                tagger.GenerateTags(""); // This will just reset the regions

                IOutliningManagerService outliningManagerService = componentModel.GetService<IOutliningManagerService>();

                IOutliningManager _outliningManager = outliningManagerService.GetOutliningManager(textView);
                var snapshot = textView.TextSnapshot;
                var snapshotSpan = new Microsoft.VisualStudio.Text.SnapshotSpan(snapshot, new Microsoft.VisualStudio.Text.Span(0, snapshot.Length));

                var regions = _outliningManager.GetCollapsedRegions(snapshotSpan);
                foreach (var reg in regions)
                {
                    _outliningManager.Expand(reg as ICollapsed);
                }

                textView.DisplayTextLineContainingBufferPosition(textView.Caret.Position.BufferPosition, 400.0, ViewRelativePosition.Top);
            }
        }
    }
}

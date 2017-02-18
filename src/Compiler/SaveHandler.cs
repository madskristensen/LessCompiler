using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace LessCompiler
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("LESS")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class CommandRegistration : IVsTextViewCreationListener
    {
        private IWpfTextView _view;
        private ProjectItem _projectItem;

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory { get; set; }

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public async void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            _view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            if (!DocumentService.TryGetTextDocument(_view.TextBuffer, out ITextDocument doc))
                return;

            _projectItem = VsHelpers.DTE.Solution.FindProjectItem(doc.FilePath);

            if (_projectItem?.ContainingProject != null)
            {
                if (Settings.IsEnabled(_projectItem.ContainingProject))
                    await LessCatalog.EnsureCatalog(_projectItem.ContainingProject);

                doc.FileActionOccurred += DocumentSaved;
            }
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk || !Settings.IsEnabled(_projectItem.ContainingProject))
                return;

            if (NodeProcess.IsInstalling)
            {
                VsHelpers.WriteStatus("The LESS compiler is being installed. Please try again in a few seconds...");
            }
            else if (NodeProcess.IsReadyToExecute())
            {
                var options = CompilerOptions.Parse(e.FilePath, _view.TextBuffer.CurrentSnapshot.GetText());
                await LessCatalog.EnsureCatalog(_projectItem.ContainingProject);

                if (options.Compile)
                {
                    LessCatalog.UpdateCatalog(_projectItem, options);
                    await CompilerService.CompileAsync(options);
                }
                else
                {
                    await CompilerService.CompileProjectAsync(_projectItem.ContainingProject);
                }
            }
        }
    }
}

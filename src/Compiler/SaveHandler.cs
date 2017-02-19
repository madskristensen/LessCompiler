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
        private Project _project;

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory { get; set; }

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public async void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            _view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            if (!DocumentService.TryGetTextDocument(_view.TextBuffer, out ITextDocument doc))
                return;

            _project = VsHelpers.DTE.Solution.FindProjectItem(doc.FilePath)?.ContainingProject;

            // Test for Properties to exclude misc files
            if (!_project.SupportsCompilation())
                return;

            _view.Properties.AddProperty("adornment", new LessAdornment(_view, _project));

            if (Settings.IsEnabled(_project))
                await LessCatalog.EnsureCatalog(_project);

            doc.FileActionOccurred += DocumentSaved;
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk || !Settings.IsEnabled(_project))
                return;

            if (NodeProcess.IsInstalling)
            {
                VsHelpers.WriteStatus("The LESS compiler is being installed. Please try again in a few seconds...");
            }
            else if (NodeProcess.IsReadyToExecute())
            {
                var options = CompilerOptions.Parse(e.FilePath, _view.TextBuffer.CurrentSnapshot.GetText());

                LessCatalog.UpdateFile(_project, options);

                if (await LessCatalog.EnsureCatalog(_project))
                    await CompilerService.CompileAsync(options, _project);
            }
        }
    }
}

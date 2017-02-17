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

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory { get; set; }

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public async void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            _view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            if (!DocumentService.TryGetTextDocument(_view.TextBuffer, out ITextDocument doc))
                return;

            doc.FileActionOccurred += DocumentSaved;

            if (!NodeProcess.IsReadyToExecute())
            {
                await CompilerService.Install();
            }
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            if (NodeProcess.IsReadyToExecute())
            {
                CompilerOptions options = CompilerService.GetOptions(e.FilePath, _view.TextBuffer.CurrentSnapshot.GetText());
                await CompilerService.Compile(options);
            }
        }
    }
}

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
        private NodeProcess _node;

        [Import]
        private IVsEditorAdaptersFactoryService AdaptersFactory { get; set; }

        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public async void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);

            if (!DocumentService.TryGetTextDocument(view.TextBuffer, out ITextDocument doc))
                return;

            doc.FileActionOccurred += DocumentSaved;

            _node = view.Properties.GetOrCreateSingletonProperty(() => new NodeProcess());

            if (!_node.IsReadyToExecute())
            {
                await CompilerService.Install(_node);
            }
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk)
                return;

            if (_node != null && CompilerService.ShouldCompile(e.FilePath) && _node.IsReadyToExecute())
            {
                await CompilerService.Compile(e.FilePath, _node);
            }
        }
    }
}

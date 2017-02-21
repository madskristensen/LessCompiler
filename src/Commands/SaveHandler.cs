using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;

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

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            _view = view;

            if (!DocumentService.TryGetTextDocument(view.TextBuffer, out ITextDocument doc))
                return;

            _project = VsHelpers.DTE.Solution.FindProjectItem(doc.FilePath)?.ContainingProject;

            if (!_project.SupportsCompilation())
                return;

            Settings.Changed += OnSettingsChanged;
            view.Closed += OnViewClosed;

            Microsoft.VisualStudio.Shell.ThreadHelper.Generic.BeginInvoke(DispatcherPriority.ApplicationIdle, async () =>
            {
                bool isEnabled = _project.IsLessCompilationEnabled();

                LessAdornment adornment = view.Properties.GetOrCreateSingletonProperty(() => new LessAdornment(view, _project));

                if (isEnabled && await LessCatalog.EnsureCatalog(_project))
                {
                    CompilerOptions options = LessCatalog.Catalog[_project.UniqueName].LessFiles.Keys.FirstOrDefault(l => l.InputFilePath == doc.FilePath);

                    if (options != null)
                    {
                        await adornment.Update(options);
                    }
                }
            });

            doc.FileActionOccurred += DocumentSaved;
        }

        private async void OnSettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            if (!e.Enabled)
                return;

            if (_view.Properties.TryGetProperty(typeof(LessAdornment), out LessAdornment adornment))
            {
                if (!DocumentService.TryGetTextDocument(_view.TextBuffer, out ITextDocument doc))
                    return;

                CompilerOptions options = await CompilerOptions.Parse(doc.FilePath);

                await adornment.Update(options);
            }
        }

        private async void DocumentSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.ContentSavedToDisk || !_project.IsLessCompilationEnabled())
                return;

            if (NodeProcess.IsInstalling)
            {
                VsHelpers.WriteStatus("The LESS compiler is being installed. Please try again in a few seconds...");
            }
            else if (NodeProcess.IsReadyToExecute())
            {
                CompilerOptions options = await CompilerOptions.Parse(e.FilePath, _view.TextBuffer.CurrentSnapshot.GetText());

                if (_view.Properties.TryGetProperty(typeof(LessAdornment), out LessAdornment adornment))
                {
                    await adornment.Update(options);
                }

                if (options == null || !_project.SupportsCompilation() || !_project.IsLessCompilationEnabled())
                    return;

                await LessCatalog.UpdateFile(_project, options);

                if (await LessCatalog.EnsureCatalog(_project))
                    await CompilerService.CompileAsync(options, _project);
            }
        }

        private void OnViewClosed(object sender, System.EventArgs e)
        {
            Settings.Changed -= OnSettingsChanged;
        }
    }
}

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Tasks = System.Threading.Tasks;

namespace LessCompiler
{
    internal sealed class ReCompileAll
    {
        private readonly Package _package;

        private ReCompileAll(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.ReCompileAll);
            var cmd = new OleMenuCommand(async (s, e) => { await Execute(); }, cmdId);
            commandService.AddCommand(cmd);
        }

        public static ReCompileAll Instance
        {
            get; private set;
        }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new ReCompileAll(package, commandService);
        }

        private async Tasks.Task Execute()
        {
            Project project = VsHelpers.GetActiveProject();

            if (project != null && NodeProcess.IsReadyToExecute())
            {
                await LessCatalog.EnsureCatalog(project);
                await CompilerService.CompileProjectAsync(project);
            }
        }
    }
}

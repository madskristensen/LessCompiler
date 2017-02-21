using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Tasks = System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System.Collections.Generic;

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
            if (!NodeProcess.IsReadyToExecute())
                return;

            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
            IEnumerable<IVsHierarchy> hierarchies = GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);

            foreach (IVsHierarchy hierarchy in hierarchies)
            {
                Project project = GetDTEProject(hierarchy);

                if (project.SupportsCompilation() && project.IsLessCompilationEnabled())
                {
                    if (await LessCatalog.EnsureCatalog(project))
                        await CompilerService.CompileProjectAsync(project);
                }
            }
        }

        // From http://stackoverflow.com/questions/22705089/how-to-get-list-of-projects-in-current-visual-studio-solution
        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
                yield break;

            Guid guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies enumHierarchies);
            if (enumHierarchies == null)
                yield break;

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
            }
        }

        public static Project GetDTEProject(IVsHierarchy hierarchy)
        {
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj);
            return obj as Project;
        }
    }
}

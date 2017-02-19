using EnvDTE;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LessCompiler
{
    public static class LessCatalog
    {
        private static string[] _ignore = { "\\node_modules\\", "\\bower_components\\", "\\jspm_packages\\", "\\lib\\", "\\vendor\\" };
        private static SolutionEvents _events;

        static LessCatalog()
        {
            Catalog = new Dictionary<string, ProjectMap>();
            _events = VsHelpers.DTE.Events.SolutionEvents;
            _events.AfterClosing += OnSolutionClosed;
        }

        public static Dictionary<string, ProjectMap> Catalog
        {
            get;
        }

        private static void OnSolutionClosed()
        {
            foreach (ProjectMap project in Catalog.Values)
            {
                project.Dispose();
            }

            Catalog.Clear();
        }

        // Todo: make this thread safe so it only runs once
        public static async Task<bool> EnsureCatalog(Project project)
        {
            if (Catalog.ContainsKey(project.UniqueName))
                return true;

            var map = new ProjectMap();
            await map.BuildMap(project);

            Catalog[project.UniqueName] = map;
            return true;
        }

        public static void UpdateFile(Project project, CompilerOptions options)
        {
            if (project == null || !Catalog.ContainsKey(project.UniqueName))
                return;

            ProjectMap map = Catalog[project.UniqueName];
            map.UpdateFile(options);
        }
    }
}

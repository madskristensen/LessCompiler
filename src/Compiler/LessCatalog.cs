using EnvDTE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LessCompiler
{
    public static class LessCatalog
    {
        private static SolutionEvents _events;
        private static AsyncLock _lock = new AsyncLock();

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

        public static async Task<bool> EnsureCatalog(Project project)
        {
            if (project == null)
                return false;

            if (Catalog.ContainsKey(project.UniqueName))
                return true;

            using (await _lock.LockAsync())
            {
                try
                {
                    var map = new ProjectMap();
                    await map.BuildMap(project);

                    Catalog[project.UniqueName] = map;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return false;
                }
            }

            return true;
        }

        public static async Task UpdateFile(Project project, CompilerOptions options)
        {
            if (project == null || options == null || !Catalog.TryGetValue(project.UniqueName, out ProjectMap map))
                return;

            await map.UpdateFile(options);
        }

        private static void OnSolutionClosed()
        {
            foreach (ProjectMap project in Catalog.Values)
            {
                project.Dispose();
            }

            Catalog.Clear();
        }
    }
}

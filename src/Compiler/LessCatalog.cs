using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LessCompiler
{
    public static class LessCatalog
    {
        private static string[] _ignore = { "\\node_modules\\", "\\bower_components\\", "\\jspm_packages\\", "\\lib\\", "\\vendor\\" };
        private static SolutionEvents _events;

        static LessCatalog()
        {
            Catalog = new Dictionary<string, List<CompilerOptions>>();
            _events = VsHelpers.DTE.Events.SolutionEvents;
            _events.AfterClosing += delegate { Catalog.Clear(); };
        }

        public static Dictionary<string, List<CompilerOptions>> Catalog
        {
            get;
        }

        public static bool IsBuilding
        {
            get; private set;
        }

        public static async Task EnsureCatalog(Project project)
        {
            if (IsBuilding || Catalog.ContainsKey(project.UniqueName))
                return;

            await BuildCatalog(project);
        }

        public static void UpdateCatalog(ProjectItem item, CompilerOptions options = null)
        {
            if (item?.ContainingProject == null)
                return;

            string file = item.FileNames[1];
            string fileName = Path.GetFileName(file);

            if (fileName.StartsWith("_", StringComparison.Ordinal))
                return;

            options = options ?? CompilerOptions.Parse(file);

            if (options.Compile)
            {
                string key = item.ContainingProject.UniqueName;

                if (!Catalog.ContainsKey(key))
                    Catalog[key] = new List<CompilerOptions>();

                CompilerOptions existing = Catalog[key].FirstOrDefault(o => o.InputFilePath == file);

                if (existing != null)
                    Catalog[key].Remove(existing);

                Catalog[key].Add(options);
            }
        }

        private static async Task BuildCatalog(Project project)
        {
            IsBuilding = true;
            string root = project.GetRootFolder();

            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return;

            await Task.Run(() =>
            {
                IEnumerable<string> lessFiles = FindLessFiles(root);

                foreach (string file in lessFiles)
                {
                    ProjectItem item = VsHelpers.DTE.Solution.FindProjectItem(file);

                    if (item != null)
                        UpdateCatalog(item);
                }

                IsBuilding = false;
            });
        }

        private static IEnumerable<string> FindLessFiles(string folder, List<string> files = null)
        {
            if (files == null)
                files = new List<string>();

            IEnumerable<string> lessFiles = Directory.EnumerateFiles(folder, "*.less");

            foreach (string file in lessFiles)
            {
                if (!_ignore.Any(i => file.IndexOf(i, StringComparison.OrdinalIgnoreCase) > -1))
                    files.Add(file);
            }

            IEnumerable<string> subDirs = Directory.EnumerateDirectories(folder);

            foreach (string dir in subDirs)
            {
                if (!_ignore.Any(i => dir.IndexOf(i, StringComparison.OrdinalIgnoreCase) > -1))
                    FindLessFiles(dir, files);
            }

            return files;
        }
    }
}

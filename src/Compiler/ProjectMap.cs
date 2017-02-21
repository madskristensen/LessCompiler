using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LessCompiler
{
    public class ProjectMap : IDisposable
    {
        private static string[] _ignore = { "\\node_modules\\", "\\bower_components\\", "\\jspm_packages\\", "\\lib\\", "\\vendor\\" };
        private static Regex _import = new Regex(@"@import ([""'])(?<url>[^""']+)\1|url\(([""']?)(?<url>[^""')]+)\2\)", RegexOptions.IgnoreCase);
        private ProjectItemsEvents _events;

        public ProjectMap()
        {
            _events = ((Events2)VsHelpers.DTE.Events).ProjectItemsEvents;
            _events.ItemAdded += OnProjectItemAdded;
            _events.ItemRemoved += OnProjectItemRemoved;
            _events.ItemRenamed += OnProjectItemRenamed;
        }

        public Dictionary<CompilerOptions, List<CompilerOptions>> LessFiles { get; } = new Dictionary<CompilerOptions, List<CompilerOptions>>();

        public async Task BuildMap(Project project)
        {
            if (!project.SupportsCompilation() || !project.IsLessCompilationEnabled())
                return;

            var sw = new Stopwatch();
            sw.Start();

            IEnumerable<string> lessFiles = FindLessFiles(project.ProjectItems);

            foreach (string file in lessFiles)
            {
                await AddFile(file);
            }

            sw.Stop();
            Logger.Log($"LESS file catalog for {project.Name} built in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        public async Task UpdateFile(CompilerOptions options)
        {
            CompilerOptions existing = LessFiles.Keys.FirstOrDefault(c => c == options);

            if (existing != null)
                LessFiles.Remove(existing);

            await AddFile(options.InputFilePath);
        }

        public void RemoveFile(CompilerOptions options)
        {
            if (options == null)
                return;

            if (LessFiles.ContainsKey(options))
                LessFiles.Remove(options);

            foreach (CompilerOptions file in LessFiles.Keys)
            {
                if (LessFiles[file].Contains(options))
                    LessFiles[file].Remove(options);
            }
        }

        private async Task AddFile(string lessFilePath)
        {
            if (LessFiles.Keys.Any(c => c.InputFilePath == lessFilePath))
                return;

            string lessContent = File.ReadAllText(lessFilePath);

            CompilerOptions options = await CompilerOptions.Parse(lessFilePath, lessContent);
            LessFiles.Add(options, new List<CompilerOptions>());

            await AddOption(options, lessContent);
        }

        private async Task AddOption(CompilerOptions options, string lessContent = null)
        {
            lessContent = lessContent ?? File.ReadAllText(options.InputFilePath);
            string lessDir = Path.GetDirectoryName(options.InputFilePath);

            foreach (Match match in _import.Matches(lessContent))
            {
                string childFilePath = new FileInfo(Path.Combine(lessDir, match.Groups["url"].Value)).FullName;

                if (!File.Exists(childFilePath))
                    continue;

                CompilerOptions import = LessFiles.Keys.FirstOrDefault(c => c.InputFilePath == childFilePath);

                if (import == null)
                {
                    import = await CompilerOptions.Parse(childFilePath);
                    await AddFile(childFilePath);
                }

                LessFiles[options].Add(import);
            }
        }

        private static IEnumerable<string> FindLessFiles(ProjectItems items, List<string> files = null)
        {
            if (files == null)
                files = new List<string>();

            foreach (ProjectItem item in items)
            {
                if (item.IsSupportedFile(out string filePath) && File.Exists(filePath))
                    files.Add(filePath);

                if (item.ProjectItems != null)
                    FindLessFiles(item.ProjectItems, files);
            }

            return files;
        }

        private async void OnProjectItemRenamed(ProjectItem item, string OldName)
        {
            if (!item.IsSupportedFile(out string filePath))
                return;

            LessFiles.Clear();
            await BuildMap(item.ContainingProject);
        }

        private void OnProjectItemRemoved(ProjectItem item)
        {
            if (!item.IsSupportedFile(out string filePath))
                return;

            CompilerOptions existing = LessFiles.Keys.FirstOrDefault(c => c.InputFilePath == filePath);

            RemoveFile(existing);
        }

        private async void OnProjectItemAdded(ProjectItem item)
        {
            if (!item.IsSupportedFile(out string filePath))
                return;

            await AddFile(filePath);
        }

        public void Dispose()
        {
            if (_events != null)
            {
                _events.ItemAdded -= OnProjectItemAdded;
                _events.ItemRemoved -= OnProjectItemRemoved;
                _events.ItemRenamed -= OnProjectItemRenamed;
            }

            LessFiles.Clear();
        }
    }
}

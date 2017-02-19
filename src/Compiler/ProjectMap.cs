using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
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
                        AddFile(file);
                }
            });
        }

        public void UpdateFile(CompilerOptions options)
        {
            CompilerOptions existing = LessFiles.Keys.FirstOrDefault(c => c == options);

            if (existing != null)
                LessFiles.Remove(existing);

            AddFile(options.InputFilePath);
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

        private void AddFile(string lessFilePath)
        {
            if (LessFiles.Keys.Any(c => c.InputFilePath == lessFilePath))
                return;

            string lessContent = File.ReadAllText(lessFilePath);

            var options = CompilerOptions.Parse(lessFilePath, lessContent);
            LessFiles.Add(options, new List<CompilerOptions>());

            AddOption(options, lessContent);
        }

        private void AddOption(CompilerOptions options, string lessContent = null)
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
                    import = CompilerOptions.Parse(childFilePath);
                    AddFile(childFilePath);
                }

                LessFiles[options].Add(import);
            }
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

        private async void OnProjectItemRenamed(ProjectItem item, string OldName)
        {
            if (!item.IsSupportedFile())
                return;

            LessFiles.Clear();
            await BuildMap(item.ContainingProject);
        }

        private void OnProjectItemRemoved(ProjectItem item)
        {
            if (!item.IsSupportedFile())
                return;

            CompilerOptions existing = LessFiles.Keys.FirstOrDefault(c => c.InputFilePath == item.FilePath());

            RemoveFile(existing);
        }

        private void OnProjectItemAdded(ProjectItem item)
        {
            if (!item.IsSupportedFile())
                return;

            AddFile(item.FilePath());
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

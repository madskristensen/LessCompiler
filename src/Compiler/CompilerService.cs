using EnvDTE;
using NUglify;
using NUglify.Css;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Tasks = System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Threading;

namespace LessCompiler
{
    internal static class CompilerService
    {
        public static async Tasks.Task CompileProjectAsync(Project project)
        {
            var tf = new JoinableTaskFactory(Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskContext);
            await tf.SwitchToMainThreadAsync();
            if (project == null || !LessCatalog.Catalog.TryGetValue(project.UniqueName, out ProjectMap map))
                return;

            var compileTasks = new List<Tasks.Task>();

            foreach (CompilerOptions option in map.LessFiles.Keys)
            {
                if (option.Compile)
                    compileTasks.Add(CompileSingleFileAsync(option));
            }

            await Tasks.Task.WhenAll(compileTasks);

            VsHelpers.WriteStatus($"LESS files in solution compiled");
        }

        public static async Tasks.Task CompileAsync(CompilerOptions options, Project project)
        {
            var tf = new JoinableTaskFactory(Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskContext);
            await tf.SwitchToMainThreadAsync();
            if (options == null || project == null || !LessCatalog.Catalog.TryGetValue(project.UniqueName, out ProjectMap map))
                return;

            IEnumerable<CompilerOptions> parents = map.LessFiles
                .Where(l => l.Value.Exists(c => c == options))
                .Select(l => l.Key)
                .Union(new[] { options })
                .Distinct();

            var sw = new Stopwatch();
            sw.Start();

            var compilerTaks = new List<Tasks.Task>();

            foreach (CompilerOptions parentOptions in parents.Where(p => p.Compile))
            {
                compilerTaks.Add(CompileSingleFileAsync(parentOptions));
            }

            await Tasks.Task.WhenAll(compilerTaks);

            sw.Stop();

            VsHelpers.WriteStatus($"LESS file compiled in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        private static async Tasks.Task<bool> CompileSingleFileAsync(CompilerOptions options)
        {
            try
            {
                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath);

                if (options.SourceMap)
                    VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath + ".map");

                CompilerResult result = await NodeProcess.ExecuteProcess(options);

                Logger.Log($"{result.Arguments}");

                if (result.HasError)
                {
                    Logger.Log(result.Error);
                    VsHelpers.WriteStatus($"Error compiling LESS file. See Output Window for details");
                }
                else
                {
                    AddFilesToProject(options);
                    Minify(options);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                VsHelpers.WriteStatus($"Error compiling LESS file. See Output Window for details");
                return false;
            }
        }

        public static bool SupportsCompilation(this Project project)
        {
            if (project?.Properties == null || project.IsKind(ProjectKinds.vsProjectKindSolutionFolder))
                return false;

            return true;
        }

        private static void AddFilesToProject(CompilerOptions options)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            ProjectItem item = VsHelpers.DTE.Solution.FindProjectItem(options.InputFilePath);

            if (item?.ContainingProject != null)
            {
                if (options.OutputFilePath == Path.ChangeExtension(options.InputFilePath, ".css"))
                {
                    VsHelpers.AddNestedFile(options.InputFilePath, options.OutputFilePath);
                }
                else
                {
                    VsHelpers.AddFileToProject(item.ContainingProject, options.OutputFilePath);
                }

                string mapFilePath = Path.ChangeExtension(options.OutputFilePath, ".css.map");

                if (File.Exists(mapFilePath))
                {
                    VsHelpers.AddNestedFile(options.OutputFilePath, mapFilePath);
                }
            }
        }

        public static void Minify(CompilerOptions options)
        {
            if (!options.Minify || !File.Exists(options.OutputFilePath))
                return;

            string cssContent = File.ReadAllText(options.OutputFilePath);

            var settings = new CssSettings
            {
                ColorNames = CssColor.Strict,
                CommentMode = CssComment.Important
            };

            NUglify.UglifyResult result = Uglify.Css(cssContent, settings);

            if (result.HasErrors)
                return;

            string minFilePath = Path.ChangeExtension(options.OutputFilePath, ".min.css");
            VsHelpers.CheckFileOutOfSourceControl(minFilePath);
            File.WriteAllText(minFilePath, result.Code, new UTF8Encoding(true));
            VsHelpers.AddNestedFile(options.OutputFilePath, minFilePath);
        }
    }
}

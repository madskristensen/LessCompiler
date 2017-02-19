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

namespace LessCompiler
{
    internal static class CompilerService
    {
        public static async Tasks.Task CompileProjectAsync(Project project)
        {
            if (!LessCatalog.Catalog.ContainsKey(project.UniqueName))
                return;

            ProjectMap map = LessCatalog.Catalog[project.UniqueName];
            var compileTasks = new List<Tasks.Task>();

            foreach (CompilerOptions option in map.LessFiles.Keys)
            {
                if (option.Compile)
                    compileTasks.Add(CompileSingleFile(option));
            }

            await Tasks.Task.WhenAll(compileTasks);

            VsHelpers.WriteStatus($"LESS files in solution compiled");
        }

        public static async Tasks.Task CompileAsync(CompilerOptions options, Project project)
        {
            if (options == null || !LessCatalog.Catalog.ContainsKey(project.UniqueName))
                return;

            ProjectMap map = LessCatalog.Catalog[project.UniqueName];

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
                compilerTaks.Add(CompileSingleFile(parentOptions));
            }

            await Tasks.Task.WhenAll(compilerTaks);

            sw.Stop();

            VsHelpers.WriteStatus($"LESS file compiled in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        private static async Tasks.Task<bool> CompileSingleFile(CompilerOptions options)
        {
            try
            {
                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath);
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
            if (project?.Properties == null)
                return false;

            return true;
        }

        private static void AddFilesToProject(CompilerOptions options)
        {
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

            UgliflyResult result = Uglify.Css(cssContent, settings);

            if (result.HasErrors)
                return;

            string minFilePath = Path.ChangeExtension(options.OutputFilePath, ".min.css");
            VsHelpers.CheckFileOutOfSourceControl(minFilePath);
            File.WriteAllText(minFilePath, result.Code, new UTF8Encoding(true));
            VsHelpers.AddNestedFile(options.OutputFilePath, minFilePath);
        }
    }
}

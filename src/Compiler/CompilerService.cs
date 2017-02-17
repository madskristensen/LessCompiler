using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NUglify;
using NUglify.Css;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Tasks = System.Threading.Tasks;

namespace LessCompiler
{
    internal class CompilerService
    {
        public static CompilerOptions GetOptions(string lessFilePath, string lessContent = null)
        {
            var options = new CompilerOptions(lessFilePath);

            // File name starts with a underscore
            if (Path.GetFileName(lessFilePath).StartsWith("_", StringComparison.Ordinal))
                return options;

            // File is not part of a project
            ProjectItem projectItem = VsHelpers.DTE.Solution.FindProjectItem(lessFilePath);
            if (projectItem == null || projectItem.ContainingProject == null)
                return options;

            return CompilerOptions.Parse(lessFilePath, lessContent);
        }

        public static async Tasks.Task Compile(CompilerOptions options)
        {
            if (options == null || !options.Compile)
                return;

            try
            {
                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath);
                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath + ".map");

                var sw = new Stopwatch();
                sw.Start();
                CompilerResult result = await NodeProcess.ExecuteProcess(options);
                sw.Stop();

                Logger.Log($"> {result.Arguments}");

                if (result.HasError)
                {
                    Logger.Log(result.Error);
                    VsHelpers.WriteStatus($"Error compiling LESS file. See Output Window for details");
                }
                else
                {
                    AddFilesToProject(options);

                    if (options.Minify)
                    {
                        Minify(options.OutputFilePath);
                    }
                }

                VsHelpers.WriteStatus($"LESS file compiled in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                VsHelpers.WriteStatus($"Error compiling LESS file. See Output Window for details");
            }
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

        public static void Minify(string cssFilePath)
        {
            if (!File.Exists(cssFilePath))
                return;

            string cssContent = File.ReadAllText(cssFilePath);

            var settings = new CssSettings
            {
                ColorNames = CssColor.Strict,
                CommentMode = CssComment.Important
            };

            UgliflyResult result = Uglify.Css(cssContent, settings);

            if (result.HasErrors)
                return;

            string minFilePath = Path.ChangeExtension(cssFilePath, ".min.css");
            VsHelpers.CheckFileOutOfSourceControl(minFilePath);
            File.WriteAllText(minFilePath, result.Code, new UTF8Encoding(true));
            VsHelpers.AddNestedFile(cssFilePath, minFilePath);
        }

        public static async Tasks.Task Install()
        {
            var statusbar = (IVsStatusbar)ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar));

            statusbar.FreezeOutput(0);
            statusbar.SetText($"Installing {NodeProcess.Packages} npm modules...");
            statusbar.FreezeOutput(1);

            bool success = await NodeProcess.EnsurePackageInstalled();
            string status = success ? "Done" : "Failed";

            statusbar.FreezeOutput(0);
            statusbar.SetText($"Installing {NodeProcess.Packages} npm modules... {status}");
            statusbar.FreezeOutput(1);
        }
    }
}

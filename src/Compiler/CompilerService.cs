using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Tasks = System.Threading.Tasks;
using NUglify;
using NUglify.Css;

namespace LessCompiler
{
    internal class CompilerService
    {
        public static CompilerOptions GetOptions(string lessFilePath, string lessContent = null)
        {
            var options = new CompilerOptions();

            // File name starts with a underscore
            if (Path.GetFileName(lessFilePath).StartsWith("_", StringComparison.Ordinal))
                return options;

            // File is not part of a project
            ProjectItem projectItem = VsHelpers.DTE.Solution.FindProjectItem(lessFilePath);

            if (projectItem == null || projectItem.ContainingProject == null)
                return options;

            return CompilerOptions.Parse(lessFilePath, lessContent);
        }

        public static async Tasks.Task Compile(string lessFilePath, NodeProcess node, CompilerOptions options)
        {
            if (!options.Compile)
                return;

            if (!options.WriteToDisk)
                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath);

            var sw = new Stopwatch();
            sw.Start();
            CompilerResult result = await node.ExecuteProcess(lessFilePath, options.Arguments);
            sw.Stop();

            Logger.Log($"Executed {result.Arguments}");

            if (result.HasError)
            {
                Logger.Log(result.Error);
                VsHelpers.WriteStatus($"Error compiling LESS file. See Output Window for details");
            }
            else if (options.WriteToDisk)
            {
                bool exist = File.Exists(options.OutputFilePath);

                if (exist)
                {
                    string existingCssFile = File.ReadAllText(options.OutputFilePath);

                    if (existingCssFile == result.Output)
                    {
                        VsHelpers.WriteStatus("CSS file didn't change after compilation");
                        return;
                    }
                }

                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath);
                File.WriteAllText(options.OutputFilePath, result.Output, new UTF8Encoding(true));
                VsHelpers.AddNestedFile(lessFilePath, options.OutputFilePath);
            }
            else if (!options.WriteToDisk)
            {
                ProjectItem item = VsHelpers.DTE.Solution.FindProjectItem(lessFilePath);

                if (item?.ContainingProject != null)
                {
                    VsHelpers.AddFileToProject(item.ContainingProject, options.OutputFilePath);

                    string mapFilePath = Path.ChangeExtension(options.OutputFilePath, ".css.map");

                    if (File.Exists(mapFilePath))
                    {
                        VsHelpers.AddNestedFile(options.OutputFilePath, mapFilePath);
                    }
                }
            }

            if (options.Minify)
            {
                Minify(options.OutputFilePath, result.Output);
            }

            VsHelpers.WriteStatus($"LESS file compiled in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        public static void Minify(string cssFilePath, string cssContent = null)
        {
            if (string.IsNullOrEmpty(cssContent))
            {
                cssContent = File.ReadAllText(cssFilePath);
            }

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

        public static async Tasks.Task Install(NodeProcess node)
        {
            var statusbar = (IVsStatusbar)ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar));

            statusbar.FreezeOutput(0);
            statusbar.SetText($"Installing {NodeProcess.Packages} npm modules...");
            statusbar.FreezeOutput(1);

            bool success = await node.EnsurePackageInstalled();
            string status = success ? "Done" : "Failed";

            statusbar.FreezeOutput(0);
            statusbar.SetText($"Installing {NodeProcess.Packages} npm modules... {status}");
            statusbar.FreezeOutput(1);
        }
    }
}

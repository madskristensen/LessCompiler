using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
                    string oldCss = File.ReadAllText(options.OutputFilePath);

                    if (oldCss == result.Output)
                    {
                        VsHelpers.WriteStatus("CSS file didn't change after compilation");
                        return;
                    }
                }

                VsHelpers.CheckFileOutOfSourceControl(options.OutputFilePath);
                File.WriteAllText(options.OutputFilePath, result.Output, new UTF8Encoding(true));
                VsHelpers.AddNestedFile(lessFilePath, options.OutputFilePath);
            }

            VsHelpers.WriteStatus($"LESS file compiled in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
        }

        public static void Minify(string cssFilePath)
        {

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

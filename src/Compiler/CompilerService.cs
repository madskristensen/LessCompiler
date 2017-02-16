using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using Tasks = System.Threading.Tasks;
using EnvDTE;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace LessCompiler
{
    internal class CompilerService
    {
        public static bool ShouldCompile(string lessFilePath)
        {
            // File name starts with a underscore
            if (Path.GetFileName(lessFilePath).StartsWith("_", StringComparison.Ordinal))
                return false;

            // File is not part of a project
            ProjectItem projectItem = VsHelpers.DTE.Solution.FindProjectItem(lessFilePath);

            if (projectItem == null || projectItem.ContainingProject == null)
                return false;

            // A comment with "nocompile" is found
            string less = File.ReadAllText(lessFilePath);

            if (less.IndexOf("nocompile", StringComparison.OrdinalIgnoreCase) > -1)
                return false;

            return true;
        }

        public static async Tasks.Task Compile(string lessFilePath, NodeProcess node, string args)
        {
            var sw = new Stopwatch();
            sw.Start();
            CompilerResult result = await node.ExecuteProcess(lessFilePath, args);
            sw.Stop();

            if (result.HasError)
            {
                Logger.Log(result.Error);
                VsHelpers.WriteStatus($"Error compiling LESS file. See Output Window for details");
            }
            else
            {
                string cssFilePath = Path.ChangeExtension(lessFilePath, ".css");

                bool exist = File.Exists(cssFilePath);

                if (exist)
                {
                    string oldCss = File.ReadAllText(cssFilePath);

                    if (oldCss == result.Output)
                    {
                        VsHelpers.WriteStatus("CSS file didn't change after compilation");
                        return;
                    }
                }

                VsHelpers.CheckFileOutOfSourceControl(cssFilePath);
                File.WriteAllText(cssFilePath, result.Output, new UTF8Encoding(true));
                VsHelpers.AddNestedFile(lessFilePath, cssFilePath);
                VsHelpers.WriteStatus($"LESS file compiled in {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds");
            }
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

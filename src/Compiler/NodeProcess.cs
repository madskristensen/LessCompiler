using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LessCompiler
{
    internal static class NodeProcess
    {
        public const string Packages = "less@2.7.2 less-plugin-autoprefix less-plugin-csscomb";

        private static string _installDir = Path.Combine(Path.GetTempPath(), Vsix.Name.Replace(" ", ""), Packages.GetHashCode().ToString());
        private static string _executable = Path.Combine(_installDir, "node_modules\\.bin\\lessc.cmd");

        public static bool IsInstalling
        {
            get;
            private set;
        }

        public static bool IsReadyToExecute()
        {
            return File.Exists(_executable);
        }

        public static async Task<bool> EnsurePackageInstalled()
        {
            if (IsInstalling)
                return false;

            if (IsReadyToExecute())
                return true;

            IsInstalling = true;

            bool success = await Task.Run(async () =>
             {
                 try
                 {
                     // Clean up any failed installation attempts
                     if (Directory.Exists(_installDir))
                         Directory.Delete(_installDir, true);

                     if (!Directory.Exists(_installDir))
                         Directory.CreateDirectory(_installDir);

                     string args = $"npm install {Packages} --no-optional";
                     Logger.Log(args);

                     var start = new ProcessStartInfo("cmd", $"/c {args}")
                     {
                         WorkingDirectory = _installDir,
                         UseShellExecute = false,
                         RedirectStandardOutput = true,
                         RedirectStandardError = true,
                         StandardOutputEncoding = Encoding.UTF8,
                         StandardErrorEncoding = Encoding.UTF8,
                         CreateNoWindow = true,
                     };

                     ModifyPathVariable(start);

                     using (var proc = Process.Start(start))
                     {
                         string output = await proc.StandardOutput.ReadToEndAsync();
                         string error = await proc.StandardOutput.ReadToEndAsync();

                         proc.WaitForExit();

                         if (!string.IsNullOrEmpty(output))
                             Logger.Log(output);

                         if (!string.IsNullOrEmpty(error))
                             Logger.Log(error);

                         return proc.ExitCode == 0;
                     }
                 }
                 catch (Exception ex)
                 {
                     Logger.Log(ex);
                     return false;
                 }
                 finally
                 {
                     IsInstalling = false;
                 }
             });

            return success;
        }

        public static async Task<CompilerResult> ExecuteProcess(CompilerOptions options)
        {
            if (!await EnsurePackageInstalled())
                return null;

            string fileName = Path.GetFileName(options.InputFilePath);
            string arguments = $"--no-color {options.Arguments}";

            Directory.CreateDirectory(Path.GetDirectoryName(options.OutputFilePath));

            var start = new ProcessStartInfo("cmd", $"/c \"\"{_executable}\" {arguments}\"")
            {
                WorkingDirectory = Path.GetDirectoryName(options.InputFilePath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
            };

            ModifyPathVariable(start);

            try
            {
                using (var proc = Process.Start(start))
                {
                    string error = await proc.StandardError.ReadToEndAsync();

                    proc.WaitForExit();

                    return new CompilerResult(options.OutputFilePath, error, arguments);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return new CompilerResult(options.OutputFilePath, ex.Message, arguments);
            }
        }

        private static void ModifyPathVariable(ProcessStartInfo start)
        {
            string path = start.EnvironmentVariables["PATH"];

            var process = Process.GetCurrentProcess();
            string ideDir = Path.GetDirectoryName(process.MainModule.FileName);

            if (Directory.Exists(ideDir))
            {
                string parent = Directory.GetParent(ideDir).Parent.FullName;

                string rc2Preview1Path = new DirectoryInfo(Path.Combine(parent, @"Web\External")).FullName;

                if (Directory.Exists(rc2Preview1Path))
                {
                    path += ";" + rc2Preview1Path;
                    path += ";" + rc2Preview1Path + "\\git";
                }
                else
                {
                    path += ";" + Path.Combine(ideDir, @"Extensions\Microsoft\Web Tools\External");
                    path += ";" + Path.Combine(ideDir, @"Extensions\Microsoft\Web Tools\External\git");
                }
            }

            start.EnvironmentVariables["PATH"] = path;
        }
    }
}

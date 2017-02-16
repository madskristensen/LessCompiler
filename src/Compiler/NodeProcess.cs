using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LessCompiler
{
    internal class NodeProcess
    {
        public const string Packages = "less less-plugin-autoprefix less-plugin-csscomb";

        private static string _installDir = Path.Combine(Path.GetTempPath(), Vsix.Name.Replace(" ", ""), Packages.GetHashCode().ToString());
        private static string _executable = Path.Combine(_installDir, "node_modules\\.bin\\lessc.cmd");

        public bool IsInstalling
        {
            get;
            private set;
        }

        public bool IsReadyToExecute()
        {
            return File.Exists(_executable);
        }

        public async Task<bool> EnsurePackageInstalled()
        {
            if (IsInstalling)
                return false;

            if (IsReadyToExecute())
                return true;

            bool success = await Task.Run(() =>
             {
                 IsInstalling = true;

                 try
                 {
                     if (!Directory.Exists(_installDir))
                         Directory.CreateDirectory(_installDir);

                     var start = new ProcessStartInfo("cmd", $"/c npm install {Packages} --no-optional")
                     {
                         WorkingDirectory = _installDir,
                         UseShellExecute = false,
                         RedirectStandardOutput = true,
                         CreateNoWindow = true,
                     };

                     ModifyPathVariable(start);

                     using (var proc = Process.Start(start))
                     {
                         proc.WaitForExit();
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

        public async Task<CompilerResult> ExecuteProcess(CompilerOptions options)
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

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LessCompiler
{
    public class CompilerOptions
    {
        private static Regex _regex = new Regex(@"\slessc(?<args>\s.+)(\*/)?", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static Regex _outFile = new Regex(@"\s((?:""|')(?<out>.+\.css)(?:""|')|(?<out>[^\s=]+\.css))", RegexOptions.Compiled);

        public CompilerOptions(string lessFilePath)
        {
            InputFilePath = lessFilePath;
            OutputFilePath = Path.ChangeExtension(lessFilePath, ".css");

            string inFile = Path.GetFileName(InputFilePath);
            string outFile = Path.GetFileName(OutputFilePath);

            Arguments = $"\"{inFile}\" --relative-urls --autoprefix=\">0%\" --csscomb=zen \"{outFile}\"";
        }

        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public string Arguments { get; set; }
        public bool Minify { get; set; }
        public bool Compile { get; set; }

        public static CompilerOptions Parse(string lessFilePath, string lessContent = null)
        {
            lessContent = lessContent ?? File.ReadAllText(lessFilePath);
            var options = new CompilerOptions(lessFilePath);

            // Compile
            if (lessContent.IndexOf("no-compile", StringComparison.OrdinalIgnoreCase) == -1)
                options.Compile = true;

            // Minify
            if (lessContent.IndexOf("no-minify", StringComparison.OrdinalIgnoreCase) == -1)
                options.Minify = true;

            // Arguments
            Match argsMatch = _regex.Match(lessContent, 0, Math.Min(500, lessContent.Length));
            if (argsMatch.Success)
            {
                string inFile = Path.GetFileName(options.InputFilePath);
                options.Arguments = $"\"{inFile}\" {argsMatch.Groups["args"].Value.TrimEnd('*', '/').Trim()}";
            }

            // OutputFileName
            Match outMatch = _outFile.Match(options.Arguments);
            if (argsMatch.Success && outMatch.Success)
            {
                string relative = outMatch.Groups["out"].Value.Replace("/", "\\");
                options.OutputFilePath = Path.Combine(Path.GetDirectoryName(lessFilePath), relative);
            }
            else
            {
                options.OutputFilePath = Path.ChangeExtension(lessFilePath, ".css");

                if (argsMatch.Success)
                {
                    options.Arguments += $" \"{Path.GetFileName(options.OutputFilePath)}\"";
                }
            }

            // Trim the argument list
            options.Arguments = options.Arguments.Trim();

            return options;
        }
    }
}

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
            string defaults = GetCompilerDefaults(lessFilePath, out bool minify);

            Minify = minify;
            Arguments = $"\"{inFile}\" {defaults} \"{outFile}\"";
        }

        public string InputFilePath { get; set; }
        public string OutputFilePath { get; set; }
        public string Arguments { get; set; }
        public bool Minify { get; set; } = true;
        public bool Compile { get; set; } = true;

        public static CompilerOptions Parse(string lessFilePath, string lessContent = null)
        {
            lessContent = lessContent ?? File.ReadAllText(lessFilePath);
            var options = new CompilerOptions(lessFilePath);

            // Compile
            if (Path.GetFileName(lessFilePath).StartsWith("_", StringComparison.Ordinal)
                || lessContent.IndexOf("no-compile", StringComparison.OrdinalIgnoreCase) > -1)
                options.Compile = false;

            // Minify
            if (lessContent.IndexOf("no-minify", StringComparison.OrdinalIgnoreCase) > -1)
                options.Minify = false;

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

        private static string GetCompilerDefaults(string lessFilePath, out bool minify)
        {
            minify = true;
            DirectoryInfo parent = new FileInfo(lessFilePath).Directory;

            while (parent != null)
            {
                string defaultFile = Path.Combine(parent.FullName, "less.defaults");

                if (File.Exists(defaultFile))
                {
                    string content = File.ReadAllText(defaultFile);

                    if (content.IndexOf("no-minify") > -1)
                        minify = false;

                    return content.Replace("no-minify", "").Trim();
                }

                parent = parent.Parent;
            }

            return "--relative-urls --autoprefix=\">0%\" --csscomb=zen";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CompilerOptions other))
                return false;

            return Equals(other);
        }

        public bool Equals(CompilerOptions other)
        {
            if (other == null)
                return false;

            return InputFilePath.Equals(other.InputFilePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return InputFilePath.GetHashCode();
        }

        public static bool operator ==(CompilerOptions a, CompilerOptions b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(CompilerOptions a, CompilerOptions b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return Path.GetFileName(InputFilePath);
        }
    }
}

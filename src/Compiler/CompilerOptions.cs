using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LessCompiler
{
    public class CompilerOptions
    {
        internal const string DefaultArugments = "--no-color --ru --autoprefix=\">0%\" --csscomb=zen";

        private static Regex _regex = new Regex(@"\slessc\s(?<args>.+)(\*/)?", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static Regex _outFile = new Regex(@"\s(?<out>[^\s]+\.css)", RegexOptions.Compiled);

        public string OutputFilePath { get; set; }
        public string Arguments { get; set; } = DefaultArugments;
        public bool Minify { get; set; }
        public bool Compile { get; set; }
        public bool WriteToDisk { get; private set; } = true;

        public static CompilerOptions Parse(string lessFilePath, string lessContent = null)
        {
            lessContent = lessContent ?? File.ReadAllText(lessFilePath);
            var options = new CompilerOptions();

            // Compile
            if (lessContent.IndexOf("no-compile", StringComparison.OrdinalIgnoreCase) == -1)
                options.Compile = true;

            // Minify
            if (lessContent.IndexOf("no-minify", StringComparison.OrdinalIgnoreCase) == -1)
                options.Minify = true;

            // Arguments
            Match match = _regex.Match(lessContent, 0, Math.Min(500, lessContent.Length));
            if (match.Success)
            {
                options.Arguments = match.Groups["args"].Value.Trim().TrimEnd('*', '/').TrimEnd();
            }

            // OutputFileName
            Match outMatch = _outFile.Match(options.Arguments);
            if (outMatch.Success)
            {
                options.OutputFilePath = Path.Combine(Path.GetDirectoryName(lessFilePath), outMatch.Groups["out"].Value.Replace("/", "\\"));
                options.WriteToDisk = false;
            }
            else
            {
                options.OutputFilePath = Path.ChangeExtension(lessFilePath, ".css");
            }

            return options;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LessCompiler
{
    public class CompilerOptions
    {
        private static Regex _regex = new Regex(@"(^|//|/\*)\s*less\s*:(?<args>.+)(\*/)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static List<string> _defaults = new List<string>
        {
            "--no-color",
            "--relative-urls",
            "--autoprefix=\">0%\"",
            "--csscomb=zen",
        };

        public static string Parse(string lessContent)
        {
            Match match = _regex.Match(lessContent, 0, Math.Min(500, lessContent.Length));
            var list = new List<string>(_defaults);

            if (match.Success)
            {
                string[] args = match.Groups["args"].Value.Split(new[] { " -" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string arg in args)
                {
                    string[] pair = arg.Split('=');
                    string existing = list.FirstOrDefault(a => a.StartsWith("-" + pair[0]));

                    if (existing != null)
                        list.Remove(existing);

                    list.Add("-" + arg.Trim());
                }
            }

            return string.Join(" ", list).TrimEnd('/', '*', ' ');
        }
    }
}

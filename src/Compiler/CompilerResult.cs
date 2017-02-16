namespace LessCompiler
{
    public class CompilerResult
    {
        public CompilerResult(string outputFile, string error, string arguments)
        {
            OutputFile = outputFile;
            Error = error;
            Arguments = "lessc.cmd " + arguments;
        }

        public string OutputFile { get; set; }
        public string Error { get; }
        public string Arguments { get; }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(Error); }
        }
    }
}

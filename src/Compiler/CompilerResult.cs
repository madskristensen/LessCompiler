namespace LessCompiler
{
    public class CompilerResult
    {
        public CompilerResult(string output, string error, string arguments)
        {
            Output = output;
            Error = error;
            Arguments = arguments;
        }

        public string Output { get; }
        public string Error { get; }
        public string Arguments { get; }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(Error); }
        }
    }
}

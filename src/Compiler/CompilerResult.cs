namespace LessCompiler
{
    public class CompilerResult
    {
        public CompilerResult(string output, string error)
        {
            Output = output;
            Error = error;
        }

        public string Output { get; }
        public string Error { get; }

        public bool HasError
        {
            get { return !string.IsNullOrEmpty(Error); }
        }
    }
}

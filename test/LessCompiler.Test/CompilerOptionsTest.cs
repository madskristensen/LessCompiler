using LessCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace LessCompilerTest
{
    [TestClass]
    public class CompilerOptionsTest
    {
        private string _lessFilePath;

        [TestInitialize]
        public void Setup()
        {
            _lessFilePath = new FileInfo("..\\..\\artifacts\\autoprefix.less").FullName;
        }

        [TestMethod]
        public async Task OverrideDefaults()
        {
            CompilerOptions args = await CompilerOptions.Parse(_lessFilePath, "// lessc --compress --csscomb=yandex --autoprefix=\">1%\"");

            Assert.AreEqual("\"autoprefix.less\" --compress --csscomb=yandex --autoprefix=\">1%\" \"autoprefix.css\"", args.Arguments);
        }

        [TestMethod]
        public async Task ShorthandSyntax()
        {
            CompilerOptions options = await CompilerOptions.Parse(_lessFilePath, "/* lessc -x out/hat.css */");

            Assert.AreEqual("\"autoprefix.less\" -x out/hat.css", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out\\hat.css"));
        }

        [TestMethod]
        public async Task NoCompileNoMinify()
        {
            CompilerOptions options = await CompilerOptions.Parse(_lessFilePath, "/* no-compile no-minify */");

            Assert.AreEqual("\"autoprefix.less\" --relative-urls --autoprefix=\"> 1%\" \"autoprefix.css\"", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsFalse(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public async Task NoCompileNoMinifyPlusCustom()
        {
            CompilerOptions options = await CompilerOptions.Parse(_lessFilePath, "/* no-compile no-minify lessc -ru */");

            Assert.AreEqual("\"autoprefix.less\" -ru \"autoprefix.css\"", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsFalse(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public async Task OnlyOutFile()
        {
            CompilerOptions options = await CompilerOptions.Parse(_lessFilePath, "/* lessc out.css */");

            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out.css"));
            Assert.IsTrue(options.Compile);
            Assert.IsTrue(options.Minify);
        }

        [TestMethod]
        public async Task OutFileWithSpaces()
        {
            CompilerOptions options = await CompilerOptions.Parse(_lessFilePath, "/* lessc --source-map=foo.css.map \"out file.css\" */");

            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out file.css"));
            Assert.IsTrue(options.Compile);
            Assert.IsTrue(options.Minify);
        }

        [TestMethod]
        public async Task SourceMapOnly()
        {
            CompilerOptions options = await CompilerOptions.Parse(_lessFilePath, "/* lessc --source-map=foo.css.map */");

            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsTrue(options.Compile);
            Assert.IsTrue(options.Minify);
        }

        [TestMethod]
        public async Task CustomDefaults()
        {
            string lessFile = new FileInfo("..\\..\\artifacts\\defaults\\customdefaults.less").FullName;
            CompilerOptions options = await CompilerOptions.Parse(lessFile);

            Assert.AreEqual("\"customdefaults.less\" --source-map \"customdefaults.css\"", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(lessFile, ".css"));
            Assert.IsTrue(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public async Task Underscore()
        {
            string lessFile = new FileInfo("..\\..\\artifacts\\_underscore.less").FullName;
            CompilerOptions options = await CompilerOptions.Parse(lessFile);

            Assert.IsFalse(options.Compile);

        }
    }
}

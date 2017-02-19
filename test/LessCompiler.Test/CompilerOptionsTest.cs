using LessCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace LessCompilerTest
{
    [TestClass]
    public class CompilerOptionsTest
    {
        private string _lessFilePath;

        [TestInitialize]
        public void Setup()
        {
            _lessFilePath = Path.Combine(Path.GetTempPath(), "foo.less");
        }

        [TestMethod]
        public void OverrideDefaults()
        {
            var args = CompilerOptions.Parse(_lessFilePath, "// lessc --compress --csscomb=yandex --autoprefix=\">1%\"");

            Assert.AreEqual("\"foo.less\" --compress --csscomb=yandex --autoprefix=\">1%\" \"foo.css\"", args.Arguments);
        }

        [TestMethod]
        public void ShorthandSyntax()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* lessc -x out/hat.css */");

            Assert.AreEqual("\"foo.less\" -x out/hat.css", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out\\hat.css"));
        }

        [TestMethod]
        public void NoCompileNoMinify()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* no-compile no-minify */");

            Assert.AreEqual("\"foo.less\" --relative-urls --autoprefix=\">0%\" --csscomb=zen \"foo.css\"", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsFalse(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public void NoCompileNoMinifyPlusCustom()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* no-compile no-minify lessc -ru */");

            Assert.AreEqual("\"foo.less\" -ru \"foo.css\"", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsFalse(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public void OnlyOutFile()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* lessc out.css */");

            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out.css"));
            Assert.IsTrue(options.Compile);
            Assert.IsTrue(options.Minify);
        }

        [TestMethod]
        public void OutFileWithSpaces()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* lessc --source-map=foo.css.map \"out file.css\" */");

            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out file.css"));
            Assert.IsTrue(options.Compile);
            Assert.IsTrue(options.Minify);
        }

        [TestMethod]
        public void SourceMapOnly()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* lessc --source-map=foo.css.map */");

            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsTrue(options.Compile);
            Assert.IsTrue(options.Minify);
        }

        [TestMethod]
        public void CustomDefaults()
        {
            string lessFile = new FileInfo("..\\..\\artifacts\\defaults\\customdefaults.less").FullName;
            var options = CompilerOptions.Parse(lessFile);

            Assert.AreEqual("\"customdefaults.less\" --source-map \"customdefaults.css\"", options.Arguments);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(lessFile, ".css"));
            Assert.IsTrue(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public void Underscore()
        {
            string lessFile = new FileInfo("..\\..\\artifacts\\_underscore.less").FullName;
            var options = CompilerOptions.Parse(lessFile);

            Assert.IsFalse(options.Compile);

        }
    }
}

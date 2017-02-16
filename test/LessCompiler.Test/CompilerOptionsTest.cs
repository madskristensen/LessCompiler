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
            _lessFilePath = Path.Combine(Path.GetTempPath(), "foo.less");
        }

        [TestMethod]
        public void OverrideDefaults()
        {
            var args = CompilerOptions.Parse(_lessFilePath, "// lessc --compress --csscomb=yandex --autoprefix=\">1%\"");

            Assert.AreEqual("--compress --csscomb=yandex --autoprefix=\">1%\"", args.Arguments);
        }

        [TestMethod]
        public void ShorthandSyntax()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* lessc -x out/hat.css */");

            Assert.AreEqual("-x out/hat.css", options.Arguments);
            Assert.IsFalse(options.WriteToDisk);
            Assert.AreEqual(options.OutputFilePath, Path.Combine(Path.GetDirectoryName(_lessFilePath), "out\\hat.css"));
        }

        [TestMethod]
        public void NoCompileNoMinify()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* no-compile no-minify */");

            Assert.AreEqual(CompilerOptions.DefaultArugments, options.Arguments);
            Assert.IsTrue(options.WriteToDisk);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsFalse(options.Compile);
            Assert.IsFalse(options.Minify);
        }

        [TestMethod]
        public void NoCompileNoMinifyPlusCustom()
        {
            var options = CompilerOptions.Parse(_lessFilePath, "/* no-compile no-minify lessc -ru */");

            Assert.AreEqual("-ru", options.Arguments);
            Assert.IsTrue(options.WriteToDisk);
            Assert.AreEqual(options.OutputFilePath, Path.ChangeExtension(_lessFilePath, ".css"));
            Assert.IsFalse(options.Compile);
            Assert.IsFalse(options.Minify);
        }
    }
}

using LessCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace LessCompilerTest
{
    [TestClass]
    public class CompilerOptionsTest
    {
        [TestMethod]
        public void OverrideDefaults()
        {
            string args = CompilerOptions.Parse("// less: --compress --csscomb=yandex --autoprefix=\">1%\"");

            Assert.AreEqual("--no-color --relative-urls --compress --csscomb=yandex --autoprefix=\">1%\"", args);
        }

        [TestMethod]
        public void ShorthandSyntax()
        {
            string args = CompilerOptions.Parse("/* less: -x out/hat.css */");

            Assert.AreEqual("--no-color --relative-urls --autoprefix=\">0%\" --csscomb=zen -x out/hat.css", args);
        }
    }
}

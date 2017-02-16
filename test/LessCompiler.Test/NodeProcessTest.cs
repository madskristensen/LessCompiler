using LessCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace LessCompilerTest
{
    [TestClass]
    public class NodeProcessTest
    {
        [TestMethod]
        public async Task AutoPrefix()
        {
            CompilerResult result = await Execute("autoprefix.less");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual("body {\n  -webkit-transition: ease;\n  -moz-transition: ease;\n  -o-transition: ease;\n  transition: ease;\n}\n", result.Output);
        }

        [TestMethod]
        public async Task UndefinedVariable()
        {
            CompilerResult result = await Execute("undefined-variable.less");

            Assert.IsTrue(result.HasError);
            Assert.IsTrue(string.IsNullOrEmpty(result.Output));
        }

        private static async Task<CompilerResult> Execute(string fileName, string args = "--no-color --relative-urls --autoprefix=\">0%\" --csscomb=zen")
        {
            var less = new FileInfo("..\\..\\artifacts\\" + fileName);
            var node = new NodeProcess();
            return await node.ExecuteProcess(less.FullName, args);
        }
    }
}

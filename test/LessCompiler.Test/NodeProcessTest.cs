using LessCompiler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace LessCompilerTest
{
    [TestClass]
    public class NodeProcessTest
    {
        [TestCleanup]
        public void Cleanup()
        {
            var dir = new DirectoryInfo("..\\..\\artifacts\\");

            foreach (FileInfo cssFile in dir.GetFiles("*.css*"))
            {
                cssFile.Delete();
            }
        }

        [TestMethod]
        public async Task AutoPrefix()
        {
            CompilerResult result = await Execute("autoprefix.less");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual("body {\n  -webkit-transition: ease;\n  -moz-transition: ease;\n  -o-transition: ease;\n  transition: ease;\n}\n", File.ReadAllText(result.OutputFile));
        }

        [TestMethod]
        public async Task UndefinedVariable()
        {
            CompilerResult result = await Execute("undefined-variable.less");

            Assert.IsTrue(result.HasError);
            Assert.IsFalse(File.Exists(result.OutputFile));
        }

        [TestMethod]
        public async Task SourceMap()
        {
            CompilerResult result = await Execute("sourcemap.less");

            Assert.IsTrue(!result.HasError);
        }

        private static async Task<CompilerResult> Execute(string fileName)
        {
            var less = new FileInfo("..\\..\\artifacts\\" + fileName);
            var options = CompilerOptions.Parse(less.FullName);
            return await NodeProcess.ExecuteProcess(options);
        }
    }
}

using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

namespace LessCompiler
{
    public class Options : DialogPage
    {
        private const string Category = "General";

        [Category(Category)]
        [DisplayName("Enabled")]
        [Description("A master switch for the compiler.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [Category(Category)]
        [DisplayName("Compiler Mode")]
        [Description("Enable or disable the compiler or set it in PerProject mode.")]
        [DefaultValue(CompilerMode.AlwaysOn)]
        [TypeConverter(typeof(EnumConverter))]
        public CompilerMode Mode { get; set; } = CompilerMode.AlwaysOn;
    }

    public enum CompilerMode
    {
        AlwaysOn,
        PerProject
    }
}

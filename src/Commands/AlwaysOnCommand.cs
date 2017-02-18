using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace LessCompiler
{
    internal sealed class AlwaysOnCommand
    {
        private readonly Package _package;

        private AlwaysOnCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var cmdId = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.AlwaysOn);
            var cmd = new OleMenuCommand(Execute, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static AlwaysOnCommand Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new AlwaysOnCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Checked = LessCompilerPackage.Options.Mode == CompilerMode.AlwaysOn;
        }

        private void Execute(object sender, EventArgs e)
        {
            if (LessCompilerPackage.Options.Mode == CompilerMode.AlwaysOn)
                LessCompilerPackage.Options.Mode = CompilerMode.PerProject;
            else
                LessCompilerPackage.Options.Mode = CompilerMode.AlwaysOn;

            LessCompilerPackage.Options.SaveSettingsToStorage();
        }
    }
}

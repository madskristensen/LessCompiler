using System;
using System.ComponentModel.Design;
using System.IO;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace LessCompiler
{
    internal sealed class EnableCommand
    {
        private readonly Package _package;

        private EnableCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var cmdId = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.Enabled);
            var cmd = new OleMenuCommand(Execute, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static EnableCommand Instance
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
            Instance = new EnableCommand(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            string text = "Enable LESS Compiler";

            if (LessCompilerPackage.Options.Enabled)
                text = "Disable LESS Compiler";

            button.Text = text;
        }

        private void Execute(object sender, EventArgs e)
        {
            LessCompilerPackage.Options.Enabled = !LessCompilerPackage.Options.Enabled;
            LessCompilerPackage.Options.SaveSettingsToStorage();
        }
    }
}

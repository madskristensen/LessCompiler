using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using EnvDTE;

namespace LessCompiler
{
    internal sealed class EnableCommand
    {
        private readonly Package _package;

        private EnableCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            var cmdId = new CommandID(PackageGuids.guidPackageCmdSet, PackageIds.cmdEnabled);
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
            Project project = VsHelpers.DTE.SelectedItems.Item(1).Project;

            if (Settings.IsEnabled(project))
                text = "Disable LESS Compiler";

            button.Text = text;
        }

        private void Execute(object sender, EventArgs e)
        {
            Project project = VsHelpers.DTE.SelectedItems.Item(1).Project;
            bool isEnabled = Settings.IsEnabled(project);
            Settings.Enable(project, !isEnabled);
        }
    }
}

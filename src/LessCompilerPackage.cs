using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Tasks = System.Threading.Tasks;
using System.ComponentModel.Design;

namespace LessCompiler
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(PackageGuids.guidAutoLoadString)]
    [ProvideUIContextRule(PackageGuids.guidAutoLoadString, Vsix.Id,
         "WAP | WebSite | DotNetCoreWeb | ProjectK | Cordova | Node| Less",
        new string[] {
            "WAP",
            "WebSite",
            "DotNetCoreWeb",
            "ProjectK",
            "Cordova",
            "Node",
            "Less"
        },
        new string[] {
            "ActiveProjectFlavor:{349C5851-65DF-11DA-9384-00065B846F21}",
            "ActiveProjectFlavor:{E24C65DC-7377-472B-9ABA-BC803B73C61A}",
            "ActiveProjectFlavor:{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}",
            "ActiveProjectCapability:DotNetCoreWeb",
            "ActiveProjectCapability:DependencyPackageManagement",
            "ActiveProjectFlavor:{3AF33F2E-1136-4D97-BBB7-1795711AC8B8}",
            "HierSingleSelectionName:.less$"
        })]
    public sealed class LessCompilerPackage : AsyncPackage
    {
        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                EnableCommand.Initialize(this, commandService);
            }
        }
    }

    [Guid("b131bb22-309e-4d64-a3a3-03fb6d30e0c3")]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class NpmInstallerPackage : AsyncPackage
    {
        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            if (!NodeProcess.IsReadyToExecute())
            {
                await NodeProcess.EnsurePackageInstalled();
            }
        }
    }
}

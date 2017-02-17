using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Tasks = System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace LessCompiler
{
    [Guid(PackageGuids.guidPackageString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class LessCompilerPackage : AsyncPackage
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

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace DestallMaterials.Extensions.VisualStudio.Blazor
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(DestallMaterials.Extensions.VisualStudio.Blazor.Package.PackageGuidString)]
    public sealed class Package : AsyncPackage
    {
        /// <summary>
        /// DestallMaterials.Extensions.VisualStudio.BlazorPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "db902ba6-20cf-436a-8743-8b52438f6bbe";

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress
        )
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }
}

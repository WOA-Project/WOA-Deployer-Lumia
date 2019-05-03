using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Exceptions;
using Deployer.Tasks;
using Grace.DependencyInjection.Attributes;
using Serilog;

namespace Deployer.Lumia.DiskPreparers
{
    [Metadata("Name", "Keep Windows 10 Phone")]
    [Metadata("Order", 0)]
    public class KeepMobileOSDiskLayoutPreparer : LumiaDiskLayoutPreparer
    {
        public KeepMobileOSDiskLayoutPreparer(IDeploymentContext context, IEnumerable<ISpaceAllocator<IPhone>> spaceAllocators, IPartitionCleaner cleaner) : base(context, cleaner)
        {
            this.spaceAllocators = spaceAllocators;
        }

        private readonly IEnumerable<ISpaceAllocator<IPhone>> spaceAllocators;

        public ByteSize SizeReservedForWindows { get; set; } = ByteSize.FromGigaBytes(18);

        private async Task AllocateSpace(ByteSize requiredSize)
        {
            Log.Information("Verifying available space");
            Log.Verbose("Verifying the available space...");
            Log.Verbose("We will need {Size} of free space for Windows", requiredSize);

            var hasEnoughSpace = await Phone.HasEnoughSpace(requiredSize);
            if (!hasEnoughSpace)
            {
                Log.Verbose("There's not enough space in the phone. We will try to allocate it automatically");

                var success = await spaceAllocators.ToObservable()
                    .Select(x => Observable.FromAsync(() => x.TryAllocate(Phone, requiredSize)))
                    .Merge(1)
                    .Any(successful => successful);

                if (!success)
                {
                    Log.Verbose("Allocation attempt failed");
                    throw new NotEnoughSpaceException($"Could not allocate {requiredSize} on the phone. Please, try to allocate the necessary space manually and retry.");
                }

                Log.Verbose("Space allocated correctly");
            }
            else
            {
                Log.Verbose("We have enough available space to deploy Windows");
            }
        }

        protected override Task AllocateSpace()
        {
            return AllocateSpace(SizeReservedForWindows);
        }
    }
}
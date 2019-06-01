using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Serilog;

namespace Deployer.Lumia
{
    public class ExistingDeploymentCleaner : IExistingDeploymentCleaner
    {
        private IDisk disk;

        public async Task Clean(IPhone toClean)
        {
            Log.Information("Performing cleanup of possible existing deployments");

            disk = await toClean.GetDeviceDisk();

            using (var context = await GptContextFactory.Create(disk.Number, FileAccess.ReadWrite))
            {
                RemoveWellKnownPartitions(context);

                // This code is not there because it's considered unsafe. It's here just in case it's useful for a rare case. 
                // RemovePartitionsAfterData(context);
            }
         
            Log.Information("Cleanup done");

            await disk.Refresh();
        }

        private static void RemoveWellKnownPartitions(GptContext context)
        {
            Log.Debug("Removing well-known partitions");
            context.RemoveExisting(PartitionName.System);
            context.RemoveExisting(PartitionName.Reserved);
            context.RemoveExisting(PartitionName.Windows);
            Log.Debug("Well-known partitions removed");
        }

        // ReSharper disable once UnusedMember.Local
        private static void RemovePartitionsAfterData(GptContext context)
        {
            Log.Debug("Trying to remove partitions created by older version of Deployer");

            var toRemove = GetPartitionsAfter(PartitionName.Data, context).ToList();

            if (toRemove.Any(IsForbidden))
            {
                throw new FileSystemException("Attempt to delete a forbidden partition. Aborting.");
            }
            
            foreach (var partition in toRemove)
            {
                context.Delete(partition);
            }
        }

        private static bool IsForbidden(Partition partition)
        {
            var forbidden = new[]
            {
                "DPP",
                "MODEM_FSG",
                "MODEM_FS1",
                "MODEM_FS2",
                "MODEM_FSC",
                "DDR",
                "SEC",
                "APDP",
                "MSADP",
                "DPO",
                "SSD",
                "DBI",
                "UEFI_BS_NV",
                "UEFI_NV",
                "UEFI_RT_NV",
                "UEFI_RT_NV_RPMB",
                "BOOTMODE",
                "LIMITS",
                "BACKUP_BS_NV",
                "BACKUP_SBL1",
                "BACKUP_SBL2",
                "BACKUP_SBL3",
                "BACKUP_PMIC",
                "BACKUP_DBI",
                "BACKUP_UEFI",
                "BACKUP_RPM",
                "BACKUP_QSEE",
                "BACKUP_QHEE",
                "BACKUP_TZ",
                "BACKUP_HYP",
                "BACKUP_WINSECAPP",
                "BACKUP_TZAPPS",
                "SVRawDump",
                "IS_UNLOCKED",
                "HACK"
            };

            if (forbidden.Any(x => x.Equals(partition.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static IEnumerable<Partition> GetPartitionsAfter(string partitionName, GptContext c)
        {
            var data = c.Get(partitionName);
            var indexOfData = c.Partitions.IndexOf(data);
            if (indexOfData <= 0)
            {
                return Enumerable.Empty<Partition>();
            }

            var toRemove = c.Partitions
                .Skip(indexOfData + 1)
                .ToList();

            return toRemove;
        }
    }
}

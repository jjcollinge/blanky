using Microsoft.ServiceFabric.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Blanky.Utils
{
    class BackupConfig
    {
        public BackupConfig(string blobStorageKey,
                            string containerName,
                            string blobName,
                            int backupInterval)
        {
            // Must be fully intialised on creation
            BlobStorageKey = blobStorageKey;
            BlobName = blobName;
            ContainerName = containerName;
            BackupInterval = backupInterval;
        }

        public string BlobStorageKey { get; set; }
        public string BlobName { get; set; }
        public string ContainerName { get; set; }
        public int BackupInterval { get; set; }
    }

    class BackupUtility
    {
        private Timer _timer;
        private IReliableStateManager _stateManager;
        private BackupConfig _backupConfig;

        public BackupUtility(ref IReliableStateManager stateManager, BackupConfig config, ElapsedEventHandler callback)
        {
            _stateManager = stateManager;
            _backupConfig = config;
            _timer = new Timer();
            _timer.Elapsed += callback;
            _timer.Interval = _backupConfig.BackupInterval;
        }

        private async void OnTimerFired(object sender, EventArgs e)
        {
            _timer.Enabled = false;
            await _stateManager.BackupAsync(backupToBlob);
        }

        private CloudBlockBlob getBlockBlob(BackupConfig config)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[config.BlobStorageKey].ConnectionString;
            var containerName = ConfigurationManager.AppSettings[config.ContainerName];
            var blobName = ConfigurationManager.AppSettings[config.BlobName];

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            return blockBlob;
        }

        private Task<bool> backupToBlob(BackupInfo backupInfo)
        {
            var success = true;
            try
            {
                var blob = getBlockBlob(_backupConfig);

                using (var fileStream = System.IO.File.OpenRead(Directory.GetFiles(backupInfo.Directory).First()))
                {
                    blob.UploadFromStream(fileStream);
                }
            }
            catch(Exception ex)
            {
                ServiceEventSource.Current.Message("ERROR: {0}", ex);
                success = false;
            }

            _timer.Enabled = false;
            return Task.FromResult(success);
        }

        public Task<bool> RestoreFromBackup(string localFilePath, BackupConfig restoreConfig)
        {
            var success = true;
            try
            {
                var blob = getBlockBlob(restoreConfig);

                using (var fileStream = System.IO.File.OpenWrite(localFilePath))
                {
                    blockBlob.DownloadToStream(fileStream);
                }

                await this.StateManager.RestoreAsync(Directory.GetParent(filePath));
            }
            catch(Exception ex)
            {
                ServiceEventSource.Current.Message("ERROR: {0}", ex);
                success = false;
            }

            return Task.FromResult(success);
        }

        public void UpdateBackupConfig(BackupConfig config)
        {
            _backupConfig = config;
        }
    }
}

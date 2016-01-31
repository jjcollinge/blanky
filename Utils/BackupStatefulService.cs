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
            _blobStorageKey = blobStorageKey;
            _blobName = blobName;
            _containerName = containerName;
            _backupInterval = backupInterval;
        }

        // READ-ONLY
        public int BlobStorageKey { get { return _blobStorageKey; } private set; }
        public int ContainerName { get { return _containerName; } private set; }
        public int BlobName { get { return _blobName; } private set; }
        public int BackupInterval { get { return _backupInterval; } private set; }

        // Data members
        private string _blobStorageKey;
        private string _containerName;
        private string _blobName;
        private int _backupInterval;
    }

    class BackupUtility
    {
        private Timer _timer;
        private IReliableStateManager _stateManager;
        private BackupConfig _config;

        public BackupUtility(ref IReliableStateManager stateManager, BackupConfig config, ElapsedEventHandler callback)
        {
            _stateManager = stateManager;
            _config = config;
            _timer = new Timer();
            _timer.Elapsed += callback;
            _timer.Interval = _config.BackupInterval;
        }

        private async void OnTimerFired(object sender, EventArgs e)
        {
            _timer.Enabled = false;
            await _stateManager.BackupAsync(backupToBlob);
        }

        private CloudBlockBlob getBlockBlob()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[_config.BlobStorageKey].ConnectionString;
            var containerName = ConfigurationManager.AppSettings[_config.ContainerName];
            var blobName = ConfigurationManager.AppSettings[_config.BlobName];

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
                var blob = getBlockBlob();

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

        public Task<bool> RestoreFromBackup(string filePath)
        {
            var success = true;
            try
            {
                var blob = getBlockBlob();

                using (var fileStream = System.IO.File.OpenWrite(filePath))
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
    }
}

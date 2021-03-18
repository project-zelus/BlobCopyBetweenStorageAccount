using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Funcs_DataMovement.Logging;
using Funcs_DataMovement.Models;
using Funcs_DataMovement.Utils;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Funcs_DataMovement
{
    public static class MessageReceiver {
        [FunctionName("MessageReceiver")]
        public static void Run([ServiceBusTrigger("all_files", "mysub", Connection = "JPOServiceBus")]string sbmsg, ILogger log){
            JPOFileInfo fileInfo = JsonConvert.DeserializeObject<JPOFileInfo>(sbmsg);
            var sourceBlobAccount = Environment.GetEnvironmentVariable("source_blob_account"); //Dest Blob Account
            var destBlobAccount = Environment.GetEnvironmentVariable("dest_blob_account"); //Dest Blob Account
            var sourceClient = new BlobServiceClient(sourceBlobAccount);
            var destClient = new BlobServiceClient(destBlobAccount);

            //Get rererence to Source Blob
            var sourceContainer = sourceClient.GetBlobContainerClient(fileInfo.source);
            var sourceBlob = sourceContainer.GetBlobClient(fileInfo.fileName);

            //Get or Create a reference to destination Blob Container and Blob
            var destContainer = destClient.GetBlobContainerClient(fileInfo.destination);
            var destBlob = destContainer.GetBlobClient(fileInfo.fileName);

            CopyBlobAsync(sourceContainer, destContainer, fileInfo, log).GetAwaiter().GetResult();

            log.LoggerInfo($"---- Received message: {JsonConvert.SerializeObject(fileInfo)}", fileInfo);
            log.LoggerInfo("Got message", fileInfo);
        }


        private static async Task CopyBlobAsync(BlobContainerClient container, BlobContainerClient destContainer, JPOFileInfo info, ILogger log) {
            try {
                // Get the name of the first blob in the container to use as the source.
                string blobName = info.fileName;

                // Create a BlobClient representing the source blob to copy.
                BlobClient sourceBlob = container.GetBlobClient(blobName);

                // Ensure that the source blob exists.
                if (await sourceBlob.ExistsAsync()) {
                    // Lease the source blob for the copy operation to prevent another client from modifying it.
                    BlobLeaseClient lease = sourceBlob.GetBlobLeaseClient();

                    // Specifying -1 for the lease interval creates an infinite lease.
                    //await lease.AcquireAsync(TimeSpan.FromSeconds(100));

                    // Get the source blob's properties and display the lease state.
                    BlobProperties sourceProperties = await sourceBlob.GetPropertiesAsync();
                    log.LoggerInfo($"Lease state: {sourceProperties.LeaseState}", info);

                    Uri blob_sas_uri = BlobUtilities.GetServiceSASUriForBlob(sourceBlob, container.Name, null);

                    // Get a BlobClient representing the destination blob
                    BlobClient destBlob = destContainer.GetBlobClient(blobName);//destContainer.GetBlobClient(blob_sas_uri.ToString());

                    // Start the copy operation.
                    await destBlob.StartCopyFromUriAsync(blob_sas_uri);

                    // Get the destination blob's properties and display the copy status.
                    BlobProperties destProperties = await destBlob.GetPropertiesAsync();

                    // Update the source blob's properties.
                    sourceProperties = await sourceBlob.GetPropertiesAsync();

                    if (sourceProperties.LeaseState == LeaseState.Leased) {
                        // Break the lease on the source blob.
                        await lease.BreakAsync();
                        // Update the source blob's properties to check the lease state.
                        sourceProperties = await sourceBlob.GetPropertiesAsync();
                    }
                }
            }
            catch (RequestFailedException ex) {
                log.LoggerError($"RequestFailedException: {ex.Message}", ex?.StackTrace, info);
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                throw;
            }
        }
    }

    
}

using Funcs_DataMovement.Logging;
using Funcs_DataMovement.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Funcs_DataMovement
{
    public static class FileMoveRequester {
        [FunctionName("FileMoveRequester")]
        [return: ServiceBus("all_files", Connection = "JPOServiceBus")] 
        public static string Run([BlobTrigger("outbox/{name}", Connection = "AccountMonitored")]Stream myBlob, string name, ILogger log){
            var correlationId = Guid.NewGuid();
            var msg = $"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes";
            var payload = new JPOFileInfo {
                source = "outbox",
                destination = "inbox",
                tags = "tag1, tag2, tag3",
                origin = "Elvis",
                description = "Return to sender",
                date = DateTime.Now,
                fileName = name,
                correlationId = correlationId
            };
            log.LoggerInfo(msg, payload);
            return JsonConvert.SerializeObject(payload);
        }
    }
}

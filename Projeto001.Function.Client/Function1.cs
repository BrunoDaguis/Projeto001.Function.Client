using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Projeto001.Function.Client
{
    public static class Function1
    {
        public static string connectionString = "mongodb://projeto001nosql:xfxgRabTgnuZETmnAFWWkrIElo8N8L7lXauElflmf4xF8gDsq7gQEBdfGvVzYgSWhGlpqQzwFv4P9YuSGlPpJA==@projeto001nosql.documents.azure.com:10255/?ssl=true&replicaSet=globaldb";

        [FunctionName("Function1")]
        public static async Task Run([EventHubTrigger("sqlserver.dbo.client", Connection = "ConnectionEHProjeto001", ConsumerGroup = "fncprojeto001client")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            settings.UseTls = true;

            var mongoClient = new MongoClient(settings);
            var db = mongoClient.GetDatabase("Projeto001");

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    BsonDocument document = BsonDocument.Parse(messageBody);

                    await db.GetCollection<BsonDocument>("Client").InsertOneAsync(document["payload"]["after"].AsBsonDocument);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    //await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace azf
{
    public class ServiceBusTrigger
    {
        [FunctionName("ServiceBusTrigger")]
        public async Task RunAsync([ServiceBusTrigger("ingestion", Connection = "servicebus")]string strItem, [DurableClient] IDurableEntityClient client, ILogger log)
        {
            //Deserialize SB message
            CMessage cm                                                 = JsonConvert.DeserializeObject<CMessage>(strItem);

            //Get Entity by crossing id
            var eDI                                                     = new EntityId(nameof(State), cm.CrossingID);
            
            //Get cache response
            EntityStateResponse<string> stateResponse                   = await client.ReadEntityStateAsync<string>(eDI);

            //Check if there is a difference in state vs data
            if ((cm.State == true && stateResponse.EntityState != "1") || (cm.State == false && stateResponse.EntityState != "0"))
            {
                //There is a state change send alerts
                if (cm.State)
                {
                    //Gate open
                    log.LogError($"OPEN");
                    await client.SignalEntityAsync(eDI, "open");
                }
                else
                {
                    //Gate closed
                    log.LogError($"CLOSED");
                    await client.SignalEntityAsync(eDI, "closed");
                }
            }

            log.LogInformation($"C# ServiceBus queue trigger function processed message: {strItem}");
        }

        [FunctionName("State")]
        public static void State([EntityTrigger] IDurableEntityContext ctx)
        {
            switch (ctx.OperationName.ToLowerInvariant())
            {
                case "open":
                    ctx.SetState(1);
                    break;
                case "closed":
                    ctx.SetState(0);
                    break;               
            }
        }
    }

    public class CMessage
    {
        public string CrossingID { get; set; }
        public bool State { get; set; }
    }
}

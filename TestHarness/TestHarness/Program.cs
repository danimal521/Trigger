using Azure.Messaging.ServiceBus;
using System;

public class Example
{
    public static async Task Main()
    {
        string connectionString                                         = "<YOUR SB CONNECTION>";
        string queueName                                                = "ingestion";
        await using var client                                          = new ServiceBusClient(connectionString);
        ServiceBusSender sender                                         = client.CreateSender(queueName);

        int nMinInterval                                                = 50;
        int nMaxInterval                                                = 100;
        Random rand                                                     = new Random();
        int nRnd                                                        = rand.Next(nMinInterval, nMaxInterval);
        int nCount                                                      = 0;
        bool bOpen                                                      = false;
        do
        {
            if(nCount == nRnd)
            {
                nRnd                                                    = rand.Next(nMinInterval, nMaxInterval);
                nCount                                                  = 0;

                if (bOpen)
                    bOpen                                               = false;
                else
                    bOpen                                               = true;
            }

            if (bOpen)
            {
                Console.WriteLine("open {0} {1}", nRnd, nCount);
                ServiceBusMessage message                               = new ServiceBusMessage("{'CrossingID':256,State:1}");
                await sender.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine("close {0} {1}", nRnd, nCount);
                ServiceBusMessage message                               = new ServiceBusMessage("{'CrossingID':256,State:0}");
                await sender.SendMessageAsync(message);
            }

            nCount++;
        }
        while (true);
    }
}
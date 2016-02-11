using MassTransit;
using MassTransit.AzureServiceBusTransport;
using MassTransit_AzureServiceBusDemo.Consumers;
using MassTransit_AzureServiceBusDemo.Messages;
using Microsoft.ServiceBus;
using System;
using System.Configuration;

namespace MassTransit_AzureServiceBusDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientUri = ServiceBusEnvironment.CreateServiceUri(
                    "sb"
                    , ConfigurationManager.AppSettings["ServiceBusNamespace"]
                    // this will get auto created for you
                    , ConfigurationManager.AppSettings["ServicePath"]);

            // this will get auto created for you
            var queueName = "MassTransitQueue";

            var bus = Bus.Factory.CreateUsingAzureServiceBus(sbc =>
            {
                var host = sbc.Host(clientUri, h =>
                {
                    h.SharedAccessSignature(s =>
                    {
                        s.KeyName = ConfigurationManager.AppSettings["SharedAccessPolicyName"];
                        s.SharedAccessKey = ConfigurationManager.AppSettings["SharedAccessKey"];
                        s.TokenTimeToLive = TimeSpan.FromMinutes(30);
                        s.TokenScope = TokenScope.Namespace;
                    });
                });

                // define handlers/consumers
                sbc.ReceiveEndpoint(host, queueName, endpoint =>
                {
                    endpoint.Consumer<MyMessageConsumer>();

                    endpoint.Handler<MySimpleMessage>(async context =>
                    {
                        await Console.Out.WriteLineAsync($"Received: {context.Message.Value}");
                    });

                    endpoint.Handler<MyPingMessage>(async context =>
                    {
                        await context.RespondAsync(new MyPongMessage
                        {
                            Value = string.Format("PONG with {0}", context.Message.Value)
                        });
                    });
                });
            });

            using (bus.Start())
            {
                Console.WriteLine("Starting...");

                // Publish a message (as in Pub/Sub)
                bus.Publish(new MySimpleMessage { Value = "Hello, World." });

                // Send Messages (regular queue)
                for (int i = 0; i < 10; i++)
                {
                    var sendEndpoint = bus.GetSendEndpoint(new Uri(clientUri, queueName)).Result;
                    sendEndpoint.Send(new MyMessage { Value = $"For Consumer {i}" });
                }

                // Pub/Sub Request/Response
                bus.PublishRequest(new MyPingMessage() { Value = "DATA" }, x =>
                {
                    x.Handle<MyPongMessage>(async context => await Console.Out.WriteLineAsync(context.Message.Value));
                    x.Timeout = TimeSpan.FromSeconds(5);
                });

                Console.WriteLine("Finished...");
                Console.ReadLine();
            }
        }
    }
}

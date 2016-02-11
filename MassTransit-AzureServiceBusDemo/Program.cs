using MassTransit;
using MassTransit.AzureServiceBusTransport;
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
            var bus = Bus.Factory.CreateUsingAzureServiceBus(sbc =>
            {
                var clientUri = ServiceBusEnvironment.CreateServiceUri(
                    "sb"
                    , ConfigurationManager.AppSettings["ServiceBusNamespace"]
                    // this will get auto created for you
                    , ConfigurationManager.AppSettings["ServicePath"]);

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

                // this will get auto created for you
                var queueName = "MassTransitQueue";

                // define handlers
                sbc.ReceiveEndpoint(host, queueName, endpoint =>
                {
                    endpoint.Handler<MyMessage>(async context =>
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
                // Publish a message
                bus.Publish(new MyMessage { Value = "Hello, World." });

                // Request/Response
                bus.PublishRequest(new MyPingMessage() { Value = "DATA" }, x =>
                {
                    x.Handle<MyPongMessage>(async context => await Console.Out.WriteLineAsync(context.Message.Value));
                    x.Timeout = TimeSpan.FromSeconds(5);
                });

                Console.ReadLine();
            }
        }
    }
}

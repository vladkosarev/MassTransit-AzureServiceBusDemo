using MassTransit;
using MassTransit_AzureServiceBusDemo.Messages;
using System;
using System.Threading.Tasks;

namespace MassTransit_AzureServiceBusDemo.Consumers
{
    public class MyMessageConsumer : IConsumer<MyMessage>
    {
        public async Task Consume(ConsumeContext<MyMessage> context)
        {
            await Console.Out.WriteLineAsync($"Consuming Message: {context.Message.Value}");
        }
    }
}

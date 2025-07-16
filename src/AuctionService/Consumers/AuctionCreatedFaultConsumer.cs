using Contracts;
using MassTransit;

namespace AuctionService.Consumers
{
    public class AuctionCreatedFaultConsumer : IConsumer { 
        public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
        {
            //Testing Fault handling on MessageBus Consume.
            Console.WriteLine("--> Consuming Fault Creation:");

            var exception = context.Message.Exceptions.FirstOrDefault();
            if(exception.ExceptionType == "System.ArgumentException")
            {
                context.Message.Message.Model = "FooBar";
                await context.Publish(context.Message.Message);
            }
            else
            {
                Console.WriteLine("Not an arg error - Update Error Dashboard somewhere");
            }
        }
    }
}

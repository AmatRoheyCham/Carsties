using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Model;

namespace SearchService.Consumers
{
    public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
    {
        private IMapper _mapper;

        public AuctionCreatedConsumer(IMapper mapper)
        {
            _mapper = mapper;
        }
        public async Task Consume(ConsumeContext<AuctionCreated> context)
        {
            Console.WriteLine("---> Consuming Auction Created"+ context.Message.Id);

            var item = _mapper.Map<Item>(context.Message);

            //Tring to mimic exception caught on this line below!
            if (item.Model == "Foo") throw new ArgumentException("Connot sell such a car!");

            await item.SaveAsync();
        }
    }
}

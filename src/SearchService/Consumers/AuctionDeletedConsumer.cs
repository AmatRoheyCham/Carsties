﻿using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Model;

namespace SearchService.Consumers
{
    public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
    {
        public async Task Consume(ConsumeContext<AuctionDeleted> context)
        {
            Console.WriteLine("--> Consuming AuctionDeleted");

            var result = await DB.DeleteAsync<Item>(context.Message.Id);

            if (!result.IsAcknowledged) { 
                throw new MessageException(typeof(AuctionDeleted),"Problem deleting acution");
            }
        }
    }
}

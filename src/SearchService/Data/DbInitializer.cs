﻿using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Model;
using SearchService.Services;

namespace SearchService.Data
{
    public class DbInitializer
    {
        public static async Task InitDb(WebApplication app)
        {

            await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

            await DB.Index<Item>()
                .Key(x => x.Make, KeyType.Text)
                .Key(x => x.Model, KeyType.Text)
                .Key(x => x.Color, KeyType.Text).CreateAsync();

            var count = await DB.CountAsync<Item>();

            //if (count == 0)
            //{
            //    Console.WriteLine("No Data - will attempt to seed");
            //    var itemData = await File.ReadAllTextAsync("Data/auctions.json");

            //    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            //    var items = JsonSerializer.Deserialize<List<Item>>(itemData, options);

            //    await DB.SaveAsync(items);

            //    Console.WriteLine($"Seeding Successful");
            //}

            using var scope = app.Services.CreateScope();
            var auctionSvcHttpClient = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();
            var items = await auctionSvcHttpClient.GetItemForSearchDB();
            if (items != null && items.Count > 0)
            {
                Console.WriteLine($"Found {items.Count} items to save to Search DB");
                await DB.SaveAsync(items);
            }
            else
            {
                Console.WriteLine("No items found to save to Search DB");
            }

        }
    }
}

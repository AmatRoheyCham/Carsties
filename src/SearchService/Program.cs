using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Data;
using SearchService.Model;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

//await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(builder.Configuration.GetConnectionString("MongoDbConnection")));

//await DB.Index<Item>()
//    .Key(x=>x.Make,KeyType.Text)
//    .Key(x=>x.Model,KeyType.Text)
//    .Key(x=>x.Color,KeyType.Text).CreateAsync();

app.Lifetime.ApplicationStarted.Register(async () =>
{
    try
    {
        await DbInitializer.InitDb(app);
    }
    catch (Exception e)
    {

        Console.WriteLine($"Cuaght Exception: {e}");
    }
});


app.Run();

static IAsyncPolicy<HttpResponseMessage> GetPolicy() => HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (exception, timeSpan, retryCount, context) =>
        {
            Console.WriteLine($"Delaying for {timeSpan} seconds, then making retry {retryCount}");
        });
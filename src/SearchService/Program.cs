using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;
using Polly;
using Polly.Extensions.Http;
using SearchService.Consumers;
using SearchService.Data;
using SearchService.Model;
using SearchService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(x =>
{
    x.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    x.AddConsumersFromNamespaceContaining<AuctionUpdatedConsumer>();
    x.AddConsumersFromNamespaceContaining<AuctionDeletedConsumer>();

    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
    x.UsingRabbitMq((context, cfg) =>
    {
        {
            //cfg.ConfigureEndpoints(context);
            cfg.Host("localhost", "/", h =>
            {
                h.Username(builder.Configuration["RabbitMqHost:Username"]);
                h.Password(builder.Configuration["RabbitMqHost:Password"]);
                //h.Username("rabbit");
                //h.Password("dev");
            });
            cfg.ReceiveEndpoint(
                "search-auction-created", e=>{
                    e.UseMessageRetry(r=> r.Interval(5,5));
                    e.ConfigureConsumer<AuctionCreatedConsumer>(context);
        });
            cfg.ReceiveEndpoint(
                "search-auction-updated", e=>{
                    e.UseMessageRetry(r=> r.Interval(5,5));
                    e.ConfigureConsumer<AuctionUpdatedConsumer>(context);
        });
            cfg.ReceiveEndpoint(
                "search-auction-deleted", e=>{
                    e.UseMessageRetry(r=> r.Interval(5,5));
                    e.ConfigureConsumer<AuctionDeletedConsumer>(context);
        });
        }
    });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


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
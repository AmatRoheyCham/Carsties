using AuctionService.Consumers;
using AuctionService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AuctionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AuctionDatabase")));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AuctionDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(10);

        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));

    x.UsingRabbitMq((context, cfg) =>
        {

            cfg.ConfigureEndpoints(context);
            cfg.Host("localhost", "/", h =>
            {
                h.Username(builder.Configuration["RabbitMqHost:Username"]);
                h.Password(builder.Configuration["RabbitMqHost:Password"]);
                //h.Username("rabbit");
                //h.Password("dev");
            });

        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{
    DbInitializer.Initialize(app);
}
catch (Exception e)
{

    Console.WriteLine($"Error Initializing: {e}");
}

app.Run();

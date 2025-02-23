using AccountService.Commands;
using AccountService.Handlers;
using Dapr.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSingleton(new NpgsqlConnection("Host=localhost;Port=5432;Database=banking;Username=postgres;Password=postgres"));
// builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddControllers();
builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();

app.UseCloudEvents();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapSubscribeHandler();
    endpoints.MapControllers();
});

app.MapPost("/events/customer-registered", async (HttpContext context, IMediator mediator) =>
{
    var customerEvent = await JsonSerializer.DeserializeAsync<dynamic>(context.Request.Body);
    var command = new CreateAccountCommand(
        customerEvent.FirstName.ToString(),
        customerEvent.LastName.ToString(),
        customerEvent.Email.ToString());
    await mediator.Send(command);
    return Results.Ok();
}).WithTopic("pubsub", "customer-registered");

app.Run();
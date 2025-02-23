using CustomerService.Commands;
using CustomerService.Handlers;
using CustomerService.Services;
using Dapr.AspNetCore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSingleton<KycService>();
builder.Services.AddSingleton(new NpgsqlConnection("Host=localhost;Port=5432;Database=banking;Username=postgres;Password=yourpassword"));
// builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();

app.UseCloudEvents();
app.UseRouting();
app.MapPost("/register", async (RegisterCustomerCommand command, IMediator mediator) =>
{
    await mediator.Send(command);
    return Results.Ok("Customer registration started");
});

using (var scope = app.Services.CreateScope())
{
    var dbConnection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
    await dbConnection.OpenAsync();
    using var cmd = new NpgsqlCommand(
        "CREATE TABLE IF NOT EXISTS Customers (Id SERIAL PRIMARY KEY, FirstName TEXT, LastName TEXT, Email TEXT UNIQUE, IdNumber TEXT, AccountNumber TEXT)",
        dbConnection);
    await cmd.ExecuteNonQueryAsync();
    await dbConnection.CloseAsync();
}

app.Run();
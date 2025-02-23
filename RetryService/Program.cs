using Dapr.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using RetryService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
// builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddHostedService<RetryBackgroundService>();
builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();
app.Run();
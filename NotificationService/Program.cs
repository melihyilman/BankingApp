using Dapr.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using NotificationService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddControllers();
builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();

app.UseCloudEvents();
app.UseRouting();

// Doğrudan üst düzey yönlendirme kullanıyoruz
app.MapPost("/events/account-created", async (HttpContext context, ILogger<Program> logger) =>
{
    // JSON'u anonim bir türe deserialize et
    var accountEvent = await JsonSerializer.DeserializeAsync<AccountEvent>(context.Request.Body, 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    // accountEvent artık anonim bir tür, Email ve AccountNumber string olarak erişilebilir
    logger.LogInformation("Sending notification to {Email}. Account: {AccountNumber}", 
        accountEvent.Email, accountEvent.AccountNumber);
    return Results.Ok();
}).WithTopic("pubsub", "account-created");

app.MapSubscribeHandler(); // Dapr abonelikleri için

app.Run();
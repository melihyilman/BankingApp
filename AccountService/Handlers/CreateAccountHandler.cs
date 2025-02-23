using AccountService.Commands;
using Dapr.Client;
using MediatR;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;

namespace AccountService.Handlers;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Unit>
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<CreateAccountHandler> _logger;
    private readonly NpgsqlConnection _dbConnection;
    private readonly IConnectionMultiplexer _redis;

    public CreateAccountHandler(DaprClient daprClient, ILogger<CreateAccountHandler> logger, NpgsqlConnection dbConnection, IConnectionMultiplexer redis)
    {
        _daprClient = daprClient;
        _logger = logger;
        _dbConnection = dbConnection;
        _redis = redis;
    }

    public async Task<Unit> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating account for: {Email}", request.Email);

        try
        {
            string accountNumber = "ACC" + Guid.NewGuid().ToString()[..8];
            await _dbConnection.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "UPDATE Customers SET AccountNumber = @accountNumber WHERE Email = @email",
                _dbConnection);
            cmd.Parameters.AddWithValue("accountNumber", accountNumber);
            cmd.Parameters.AddWithValue("email", request.Email);
            await cmd.ExecuteNonQueryAsync();
            await _dbConnection.CloseAsync();

            var accountEvent = new { request.Email, AccountNumber = accountNumber };
            await _daprClient.PublishEventAsync("pubsub", "account-created", accountEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account for: {Email}", request.Email);
            var redisDb = _redis.GetDatabase();
            var retryEvent = new { EventType = "account-created", Data = System.Text.Json.JsonSerializer.Serialize(new { request.FirstName, request.LastName, request.Email }) };
            await redisDb.StringSetAsync($"retry:{request.Email}", System.Text.Json.JsonSerializer.Serialize(retryEvent), TimeSpan.FromMinutes(30));
        }

        return Unit.Value;
    }
}
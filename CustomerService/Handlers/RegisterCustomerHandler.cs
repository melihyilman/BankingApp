using CustomerService.Commands;
using CustomerService.Services;
using Dapr.Client;
using MediatR;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
namespace CustomerService.Handlers;

public class RegisterCustomerHandler : IRequestHandler<RegisterCustomerCommand, Unit>
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<RegisterCustomerHandler> _logger;
    private readonly KycService _kycService;
    private readonly NpgsqlConnection _dbConnection;
    private readonly IConnectionMultiplexer _redis;

    public RegisterCustomerHandler(DaprClient daprClient, ILogger<RegisterCustomerHandler> logger, KycService kycService, NpgsqlConnection dbConnection, IConnectionMultiplexer redis)
    {
        _daprClient = daprClient;
        _logger = logger;
        _kycService = kycService;
        _dbConnection = dbConnection;
        _redis = redis;
    }

    public async Task<Unit> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering customer: {Email}", request.Email);

        try
        {
            if (!await _kycService.VerifyIdentity(request.IdNumber))
                throw new Exception("Identity verification failed");

            await _dbConnection.OpenAsync();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO Customers (FirstName, LastName, Email, IdNumber) VALUES (@firstName, @lastName, @email, @idNumber)",
                _dbConnection);
            cmd.Parameters.AddWithValue("firstName", request.FirstName);
            cmd.Parameters.AddWithValue("lastName", request.LastName);
            cmd.Parameters.AddWithValue("email", request.Email);
            cmd.Parameters.AddWithValue("idNumber", request.IdNumber);
            await cmd.ExecuteNonQueryAsync();
            await _dbConnection.CloseAsync();

            var customerEvent = new { request.FirstName, request.LastName, request.Email, request.IdNumber };
            await _daprClient.PublishEventAsync("pubsub", "customer-registered", customerEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register customer: {Email}", request.Email);
            var redisDb = _redis.GetDatabase();
            var retryEvent = new { EventType = "customer-registered", Data = System.Text.Json.JsonSerializer.Serialize(new { request.FirstName, request.LastName, request.Email, request.IdNumber }) };
            await redisDb.StringSetAsync($"retry:{request.Email}", System.Text.Json.JsonSerializer.Serialize(retryEvent), TimeSpan.FromMinutes(30));
        }

        return Unit.Value;
    }
}
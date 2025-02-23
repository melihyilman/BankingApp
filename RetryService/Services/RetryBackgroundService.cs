using Dapr.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RetryService.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace RetryService.Services;

public class RetryBackgroundService : BackgroundService
{
    private readonly DaprClient _daprClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RetryBackgroundService> _logger;

    public RetryBackgroundService(DaprClient daprClient, IConnectionMultiplexer redis, ILogger<RetryBackgroundService> logger)
    {
        _daprClient = daprClient;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var redisDb = _redis.GetDatabase();
                var keys = _redis.GetServer("localhost", 6379).Keys(pattern: "retry:*");

                foreach (var key in keys)
                {
                    var value = await redisDb.StringGetAsync(key);
                    if (value.HasValue)
                    {
                        var retryEvent = JsonSerializer.Deserialize<RetryEvent>(value);
                        _logger.LogInformation("Retrying event: {EventType} for key: {Key}", retryEvent.EventType, key);

                        try
                        {
                            await _daprClient.PublishEventAsync("pubsub", retryEvent.EventType, JsonSerializer.Deserialize<dynamic>(retryEvent.Data));
                            await redisDb.KeyDeleteAsync(key);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Retry failed for key: {Key}", key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retry background service");
            }

            await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
        }
    }
}
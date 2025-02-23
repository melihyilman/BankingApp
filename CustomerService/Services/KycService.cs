namespace CustomerService.Services;

public class KycService
{
    private readonly ILogger<KycService> _logger;

    public KycService(ILogger<KycService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> VerifyIdentity(string idNumber)
    {
        _logger.LogInformation("Verifying identity for ID: {IdNumber}", idNumber);
        await Task.Delay(100); 
        return idNumber.Length == 11;
    }
}
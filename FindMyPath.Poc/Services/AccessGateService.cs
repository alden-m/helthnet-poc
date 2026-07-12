using System.Security.Cryptography;
using System.Text;

namespace FindMyPath.Poc.Services;

/// <summary>
/// Validates the configured proof-of-concept access PIN without ever exposing it to the client.
/// </summary>
public sealed class AccessGateService
{
    public const string AuthenticationScheme = "FindMyPath.Access";
    public const string CookieName = ".FindMyPath.Access";
    public const string ConfigurationKey = "AccessGate:Pin";
    public const string DefaultPin = "n.rayan";

    private readonly byte[] _configuredPin;

    public AccessGateService(IConfiguration configuration)
    {
        var configuredPin = configuration[ConfigurationKey];
        var resolvedPin = string.IsNullOrWhiteSpace(configuredPin) ? DefaultPin : configuredPin;
        _configuredPin = Encoding.UTF8.GetBytes(resolvedPin);
    }

    public bool IsValid(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        var candidateBytes = Encoding.UTF8.GetBytes(candidate);
        return CryptographicOperations.FixedTimeEquals(_configuredPin, candidateBytes);
    }
}

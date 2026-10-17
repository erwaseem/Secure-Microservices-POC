using Polly;
using Polly.Extensions.Http;

/// <summary>
/// Reusable Polly resilience policies for outbound HttpClient calls.
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Retry up to 3 times with exponential back-off: 2 s, 4 s, 8 s.
    /// Triggers on transient HTTP errors (5xx, network failures).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    /// <summary>
    /// Opens the circuit after 5 consecutive failures; stays open for 30 s.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
}

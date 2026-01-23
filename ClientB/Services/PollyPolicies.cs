using Microsoft.Extensions.Configuration;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Messages;

namespace ClientB.Services;

public class PollyPolicies
{
    public ResiliencePipeline<string> RetryPipeline { get; }
    public ResiliencePipeline<string> CircuitBreakerPipeline { get; }

    public PollyPolicies(IConfiguration configuration)
    {
        // Retry Policy - for RetryableException
        var maxRetryAttempts = configuration.GetValue<int>("Polly:RetryPolicy:MaxRetryAttempts", 3);
        var delayBetweenRetries = configuration.GetValue<int>("Polly:RetryPolicy:DelayBetweenRetriesSeconds", 2);

        RetryPipeline = new ResiliencePipelineBuilder<string>()
            .AddRetry(new RetryStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>()
                    .Handle<RetryableException>(),
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromSeconds(delayBetweenRetries),
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    Console.WriteLine($"[POLLY RETRY] Attempt {args.AttemptNumber + 1} failed. Retrying in {delayBetweenRetries}s...");
                    Console.WriteLine($"[POLLY RETRY] Exception: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // Circuit Breaker Policy - for CircuitBreakerException
        var failureThreshold = configuration.GetValue<int>("Polly:CircuitBreaker:FailureThreshold", 3);
        var durationOfBreak = configuration.GetValue<int>("Polly:CircuitBreaker:DurationOfBreakSeconds", 30);
        var samplingDuration = configuration.GetValue<int>("Polly:CircuitBreaker:SamplingDurationSeconds", 60);

        CircuitBreakerPipeline = new ResiliencePipelineBuilder<string>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>()
                    .Handle<CircuitBreakerException>(),
                FailureRatio = 0.5,
                MinimumThroughput = failureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(samplingDuration),
                BreakDuration = TimeSpan.FromSeconds(durationOfBreak),
                OnOpened = args =>
                {
                    Console.WriteLine($"[POLLY CIRCUIT BREAKER] Circuit OPENED! Break duration: {durationOfBreak}s");
                    Console.WriteLine($"[POLLY CIRCUIT BREAKER] Exception: {args.Outcome.Exception?.Message}");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("[POLLY CIRCUIT BREAKER] Circuit CLOSED. Normal operation resumed.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("[POLLY CIRCUIT BREAKER] Circuit HALF-OPEN. Testing if service recovered...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}

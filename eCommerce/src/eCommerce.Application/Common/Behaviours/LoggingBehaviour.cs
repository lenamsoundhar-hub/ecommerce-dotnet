using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace eCommerce.Application.Common.Behaviours;

/// <summary>
/// Emits one log entry per request with its outcome and duration. Requests
/// slower than <see cref="SlowRequestThresholdMs"/> are surfaced as warnings.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var timestamp = Stopwatch.GetTimestamp();

        _logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            var elapsed = Stopwatch.GetElapsedTime(timestamp);

            if (elapsed.TotalMilliseconds > SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Long-running request {RequestName} completed in {ElapsedMilliseconds}ms",
                    requestName,
                    (long)elapsed.TotalMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Handled {RequestName} in {ElapsedMilliseconds}ms",
                    requestName,
                    (long)elapsed.TotalMilliseconds);
            }

            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "{RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                (long)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);

            throw;
        }
    }
}

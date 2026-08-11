namespace eCommerce.Api.Infrastructure;

/// <summary>
/// Cross-origin access for the SPA front end. Origins come from configuration
/// rather than being hard-coded, so a localhost dev server and a deployed front
/// end can differ without a code change.
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "eCommerceFrontend";

    public const string ConfigurationSection = "Cors:AllowedOrigins";

    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var origins = configuration.GetSection(ConfigurationSection).Get<string[]>() ?? [];

        return services.AddCors(options => options.AddPolicy(PolicyName, policy =>
        {
            if (origins.Length == 0)
            {
                // No origins configured means no cross-origin access, rather than
                // silently falling back to allowing everything.
                return;
            }

            policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                // Lets the browser reuse a preflight result instead of sending
                // OPTIONS ahead of every request.
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        }));
    }
}

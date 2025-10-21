using MultiAgentSystem.Api.Agents;

namespace MultiAgentSystem.Api.Services;

public class AgentPreloadService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentPreloadService> _logger;

    public AgentPreloadService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AgentPreloadService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if preloading is enabled
        if (!_configuration.GetValue<bool>("AgentPreload:Enabled", true))
        {
            _logger.LogInformation("Agent preloading is disabled in configuration");
            return;
        }

        // Wait a bit for the application to fully start
        var delayMs = _configuration.GetValue<int>("AgentPreload:WarmupDelayMs", 2000);
        await Task.Delay(delayMs, stoppingToken);

        if (stoppingToken.IsCancellationRequested) return;

        try
        {
            await PreloadAgentsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload agents during startup");
        }
    }

    private async Task PreloadAgentsAsync(CancellationToken cancellationToken)
    {
        var searchType = _configuration["SearchType"] ?? "AISearch";
        var useCustomRAG = searchType.Equals("BingCustomSearch", StringComparison.OrdinalIgnoreCase);

        if (!useCustomRAG)
        {
            _logger.LogInformation("Bing Custom Search Agent not configured as primary search, skipping preload");
            return;
        }

        try
        {
            _logger.LogInformation("Starting Bing Custom Search Agent preload...");

            using var scope = _serviceProvider.CreateScope();
            var bingAgent = scope.ServiceProvider.GetRequiredService<IBingCustomSearchAgent>();

            // Perform a simple warmup query
            var warmupQuery = "test connection";
            var startTime = DateTime.UtcNow;

            var result = await bingAgent.QueryAsync(warmupQuery, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Bing Custom Search Agent preload completed in {Duration}ms. Agent is ready.", duration.TotalMilliseconds);

            // Optionally perform additional warmup queries for common scenarios
            if (_configuration.GetValue<bool>("AgentPreload:ExtensiveWarmup", false))
            {
                await PerformExtensiveWarmupAsync(bingAgent, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Agent preload was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload Bing Custom Search Agent, but application will continue normally");
        }
    }

    private async Task PerformExtensiveWarmupAsync(IBingCustomSearchAgent bingAgent, CancellationToken cancellationToken)
    {
        var warmupQueries = new[]
        {
            "NAB products",
            "account information",
            "support options"
        };

        _logger.LogInformation("Performing extensive warmup with {Count} additional queries", warmupQueries.Length);

        foreach (var query in warmupQueries)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var startTime = DateTime.UtcNow;
                await bingAgent.QueryAsync(query, cancellationToken);
                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Warmup query '{Query}' completed in {Duration}ms", query, duration.TotalMilliseconds);

                // Small delay between warmup queries to avoid overwhelming the service
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Warmup query '{Query}' failed, continuing with next", query);
            }
        }

        _logger.LogInformation("Extensive warmup completed");
    }
}
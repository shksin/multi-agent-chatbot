using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System.Collections.Concurrent;

namespace MultiAgentSystem.Api.Services;

public interface IAIFoundryConnectionPool
{
    Task<PersistentAgentsClient> GetClientAsync(string endpoint);
    Task<PersistentAgent> GetAgentAsync(string endpoint, string assistantId);
}

public class AIFoundryConnectionPool : IAIFoundryConnectionPool, IDisposable
{
    private readonly ConcurrentDictionary<string, PersistentAgentsClient> _clients = new();
    private readonly ConcurrentDictionary<string, (PersistentAgent Agent, DateTime CachedAt)> _agentCache = new();
    private readonly ILogger<AIFoundryConnectionPool> _logger;
    private readonly Timer _cleanupTimer;
    private const int AGENT_CACHE_MINUTES = 30;

    public AIFoundryConnectionPool(ILogger<AIFoundryConnectionPool> logger)
    {
        _logger = logger;
        
        // Setup cleanup timer to run every 15 minutes
        _cleanupTimer = new Timer(CleanupExpiredCache, null, 
            TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
    }

    public Task<PersistentAgentsClient> GetClientAsync(string endpoint)
    {
        var client = _clients.GetOrAdd(endpoint, ep =>
        {
            _logger.LogInformation("Creating new PersistentAgentsClient for endpoint: {Endpoint}", ep);
            return new PersistentAgentsClient(ep, new DefaultAzureCredential());
        });

        return Task.FromResult(client);
    }

    public async Task<PersistentAgent> GetAgentAsync(string endpoint, string assistantId)
    {
        var cacheKey = $"{endpoint}#{assistantId}";
        
        // Check if we have a cached agent that's not expired
        if (_agentCache.TryGetValue(cacheKey, out var cached) && 
            cached.CachedAt.AddMinutes(AGENT_CACHE_MINUTES) > DateTime.UtcNow)
        {
            _logger.LogDebug("Returning cached agent for {AssistantId}", assistantId);
            return cached.Agent;
        }

        // Get or create client and fetch agent
        var client = await GetClientAsync(endpoint);
        
        try
        {
            _logger.LogDebug("Fetching agent {AssistantId} from AI Foundry", assistantId);
            var agent = await client.Administration.GetAgentAsync(assistantId);
            
            // Cache the agent
            _agentCache[cacheKey] = (agent, DateTime.UtcNow);
            _logger.LogDebug("Cached agent {AssistantId}", assistantId);
            
            return agent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent {AssistantId} from endpoint {Endpoint}", assistantId, endpoint);
            throw;
        }
    }

    private void CleanupExpiredCache(object? state)
    {
        try
        {
            var expiredKeys = _agentCache
                .Where(kvp => kvp.Value.CachedAt.AddMinutes(AGENT_CACHE_MINUTES) <= DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _agentCache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired agent cache entries", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache cleanup");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        
        // Note: PersistentAgentsClient doesn't implement IDisposable in current SDK
        // If it does in future versions, dispose clients here
        _clients.Clear();
        _agentCache.Clear();
    }
}
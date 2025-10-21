using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System.Text.Json;
using MultiAgentSystem.Api.Services;

namespace MultiAgentSystem.Api.Agents;

public interface IBingCustomSearchAgent
{
    Task<string> QueryAsync(string query, CancellationToken cancellationToken = default);
}

public class BingCustomSearchAgent : IBingCustomSearchAgent
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BingCustomSearchAgent> _logger;
    private readonly HttpClient _httpClient;
    private readonly IAIFoundryConnectionPool _connectionPool;
    
    // Thread pooling for reuse
    private readonly Queue<string> _availableThreads = new();
    private readonly Dictionary<string, DateTime> _threadLastUsed = new();
    private readonly SemaphoreSlim _threadPoolSemaphore;
    private readonly object _threadPoolLock = new object();
    
    // Response caching
    private static readonly Dictionary<string, (string Response, DateTime Expiry)> _responseCache = new();
    private static readonly object _cacheLock = new object();

    public BingCustomSearchAgent(
        IConfiguration configuration, 
        ILogger<BingCustomSearchAgent> logger, 
        HttpClient httpClient,
        IAIFoundryConnectionPool connectionPool)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _connectionPool = connectionPool;
        
        var timeoutSeconds = _configuration.GetValue<int>("HttpClient:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        
        // Initialize thread pool
        var poolSize = _configuration.GetValue<int>("AIFoundry:ThreadPoolSize", 3);
        _threadPoolSemaphore = new SemaphoreSlim(poolSize, poolSize);
    }

    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bing Custom Search Agent processing query: {Query}", query);

        try
        {
            // Check cache first if enabled
            if (_configuration.GetValue<bool>("AIFoundry:EnableResponseCaching", true))
            {
                var cachedResponse = GetCachedResponse(query);
                if (cachedResponse != null)
                {
                    _logger.LogDebug("Returning cached response for query: {Query}", query);
                    return cachedResponse;
                }
            }

            // Check if AI Foundry configuration is available
            var endpoint = _configuration["AIFoundry:ProjectEndpoint"];
            var agentName = _configuration["AIFoundry:AgentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(agentName))
            {
                return await GetDummyResponseAsync(query);
            }

            // Call the existing agent in AI Foundry with optimizations
            var agentResponse = await CallAIFoundryAgentOptimizedAsync(query, endpoint, agentName, cancellationToken);

            // Cache the response if enabled
            if (_configuration.GetValue<bool>("AIFoundry:EnableResponseCaching", true))
            {
                CacheResponse(query, agentResponse);
            }

            return agentResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Bing Custom Search Agent. Falling back to dummy response.");
            return await GetDummyResponseAsync(query);
        }
    }

    private string? GetCachedResponse(string query)
    {
        var cacheKey = GenerateCacheKey(query);
        
        lock (_cacheLock)
        {
            if (_responseCache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
            {
                return cached.Response;
            }
            
            // Cleanup expired entries
            var expiredKeys = _responseCache
                .Where(kvp => kvp.Value.Expiry <= DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var key in expiredKeys)
            {
                _responseCache.Remove(key);
            }
        }
        
        return null;
    }

    private void CacheResponse(string query, string response)
    {
        var cacheKey = GenerateCacheKey(query);
        var cacheDuration = _configuration.GetValue<int>("AIFoundry:CacheDurationMinutes", 15);
        
        lock (_cacheLock)
        {
            _responseCache[cacheKey] = (response, DateTime.UtcNow.AddMinutes(cacheDuration));
        }
    }

    private string GenerateCacheKey(string query)
    {
        // Create a simple hash of the query for caching
        return $"bing_search_{query.ToLowerInvariant().GetHashCode()}";
    }

    private async Task<string> CallAIFoundryAgentOptimizedAsync(string query, string endpoint, string agentName, CancellationToken cancellationToken)
    {
        try
        {
            var assistantId = _configuration["AIFoundry:AssistantId"];
            if (string.IsNullOrEmpty(assistantId))
            {
                return "Configuration Error: AssistantId not found.";
            }

            var useThreadPool = _configuration.GetValue<bool>("AIFoundry:UseThreadPool", true);
            
            if (useThreadPool)
            {
                return await CallWithThreadPoolAsync(query, endpoint, assistantId, cancellationToken);
            }
            else
            {
                return await CallAIFoundryAgentAsync(query, endpoint, agentName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in optimized AI Foundry call");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> CallWithThreadPoolAsync(string query, string endpoint, string assistantId, CancellationToken cancellationToken)
    {
        // Wait for available slot in thread pool
        await _threadPoolSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var threadId = GetOrCreateThread(endpoint);
            return await ExecuteOnThreadAsync(query, endpoint, assistantId, threadId, cancellationToken);
        }
        finally
        {
            _threadPoolSemaphore.Release();
        }
    }

    private string GetOrCreateThread(string endpoint)
    {
        lock (_threadPoolLock)
        {
            // Clean up old threads first
            CleanupOldThreads();
            
            // Try to get an available thread
            if (_availableThreads.Count > 0)
            {
                var threadId = _availableThreads.Dequeue();
                _threadLastUsed[threadId] = DateTime.UtcNow;
                _logger.LogDebug("Reusing thread: {ThreadId}", threadId);
                return threadId;
            }
        }
        
        // If no available threads, we'll create a new one in ExecuteOnThreadAsync
        return string.Empty;
    }

    private void CleanupOldThreads()
    {
        var maxIdleMinutes = _configuration.GetValue<int>("AIFoundry:MaxThreadIdleMinutes", 10);
        var cutoffTime = DateTime.UtcNow.AddMinutes(-maxIdleMinutes);
        
        var oldThreads = _threadLastUsed
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
            
        foreach (var threadId in oldThreads)
        {
            _threadLastUsed.Remove(threadId);
            
            // Remove from available queue if present
            var tempQueue = new Queue<string>();
            while (_availableThreads.Count > 0)
            {
                var id = _availableThreads.Dequeue();
                if (id != threadId)
                {
                    tempQueue.Enqueue(id);
                }
            }
            
            while (tempQueue.Count > 0)
            {
                _availableThreads.Enqueue(tempQueue.Dequeue());
            }
        }
        
        if (oldThreads.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} old threads", oldThreads.Count);
        }
    }

    private async Task<string> ExecuteOnThreadAsync(string query, string endpoint, string assistantId, string existingThreadId, CancellationToken cancellationToken)
    {
        try
        {
            var agentClient = await _connectionPool.GetClientAsync(endpoint);
            var agent = await _connectionPool.GetAgentAsync(endpoint, assistantId);
            
            PersistentAgentThread thread;
            
            // Use existing thread or create new one
            if (!string.IsNullOrEmpty(existingThreadId))
            {
                try
                {
                    thread = await agentClient.Threads.GetThreadAsync(existingThreadId);
                }
                catch
                {
                    // Thread might have been deleted, create new one
                    thread = await agentClient.Threads.CreateThreadAsync();
                }
            }
            else
            {
                thread = await agentClient.Threads.CreateThreadAsync();
            }

            // Add the new thread to pool for future use
            lock (_threadPoolLock)
            {
                if (!_threadLastUsed.ContainsKey(thread.Id))
                {
                    _availableThreads.Enqueue(thread.Id);
                    _threadLastUsed[thread.Id] = DateTime.UtcNow;
                }
            }

            // Create message and run
            await agentClient.Messages.CreateMessageAsync(thread.Id, MessageRole.User, query);
            var run = await agentClient.Runs.CreateRunAsync(thread.Id, agent.Id);

            // Optimized polling with exponential backoff
            await WaitForRunCompletionAsync(agentClient, thread.Id, run.Value.Id, cancellationToken);

            // Get only the latest message (more efficient)
            var response = await GetLatestResponseAsync(agentClient, thread.Id);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing on thread");
            throw;
        }
    }

    private async Task WaitForRunCompletionAsync(PersistentAgentsClient client, string threadId, string runId, CancellationToken cancellationToken)
    {
        var pollingInterval = _configuration.GetValue<int>("AIFoundry:PollingIntervalMs", 150);
        var maxTimeout = _configuration.GetValue<int>("AIFoundry:MaxPollingTimeoutMs", 20000);
        var startTime = DateTime.UtcNow;
        var currentInterval = pollingInterval;
        
        ThreadRun run;
        
        do
        {
            if ((DateTime.UtcNow - startTime).TotalMilliseconds > maxTimeout)
            {
                throw new TimeoutException($"Agent processing timeout after {maxTimeout}ms");
            }
            
            await Task.Delay(currentInterval, cancellationToken);
            run = await client.Runs.GetRunAsync(threadId, runId);
            
            // Exponential backoff with cap
            if (run.Status == RunStatus.InProgress)
            {
                currentInterval = Math.Min(currentInterval * 2, 1000); // Cap at 1 second
            }
        }
        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress);

        if (run.Status != RunStatus.Completed)
        {
            throw new Exception($"Run failed with status {run.Status}: {run.LastError?.Message}");
        }
    }

    private async Task<string> GetLatestResponseAsync(PersistentAgentsClient client, string threadId)
    {
        // Get messages in descending order to get the latest first
        var messages = client.Messages.GetMessagesAsync(
            threadId: threadId,
            order: ListSortOrder.Descending
        );

        await foreach (var message in messages)
        {
            // Skip user messages, get first assistant message  
            if (message.Role != MessageRole.User)
            {
                foreach (var contentItem in message.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        var response = textItem.Text;

                        // Process annotations
                        if (textItem.Annotations != null)
                        {
                            foreach (var annotation in textItem.Annotations)
                            {
                                if (annotation is MessageTextUriCitationAnnotation urlAnnotation)
                                {
                                    response = response.Replace(urlAnnotation.Text, 
                                        $" [{urlAnnotation.UriCitation.Title}] ({urlAnnotation.UriCitation.Uri})");
                                }
                            }
                        }

                        return response;
                    }
                }
                break; // Found assistant message, no need to continue
            }
        }

        return "No response received from agent.";
    }

    private async Task<string> CallAIFoundryAgentAsync(string query, string endpoint, string agentName, CancellationToken cancellationToken)
    {
        try
        {
            var assistantId = _configuration["AIFoundry:AssistantId"];

            if (string.IsNullOrEmpty(assistantId))
            {
                _logger.LogWarning("AssistantId not configured. Please add your AI Foundry Assistant ID to configuration.");
                return "Configuration Error: AssistantId not found. Please configure your AI Foundry Assistant ID.";
            }

            PersistentAgentsClient agentClient = await _connectionPool.GetClientAsync(endpoint);

            PersistentAgent agent = await _connectionPool.GetAgentAsync(endpoint, assistantId);

            PersistentAgentThread thread = await agentClient.Threads.CreateThreadAsync();

            //Ask a question of the Agent.
            await agentClient.Messages.CreateMessageAsync(thread.Id, MessageRole.User, query);

            //Have Agent begin processing user's question with some additional instructions associated with the ThreadRun.
            ThreadRun run = await agentClient.Runs.CreateRunAsync(thread.Id, agent.Id);

            // Wait for the agent to finish running with async polling
            var pollingInterval = _configuration.GetValue<int>("AIFoundry:PollingIntervalMs", 250);
            var maxTimeout = _configuration.GetValue<int>("AIFoundry:MaxPollingTimeoutMs", 30000);
            var startTime = DateTime.UtcNow;
            
            do
            {
                if ((DateTime.UtcNow - startTime).TotalMilliseconds > maxTimeout)
                {
                    throw new TimeoutException($"Agent processing timeout after {maxTimeout}ms");
                }
                
                await Task.Delay(pollingInterval, cancellationToken);
                run = await agentClient.Runs.GetRunAsync(thread.Id, run.Id);
            }
            while (run.Status == RunStatus.Queued
                || run.Status == RunStatus.InProgress);

            // Confirm that the run completed successfully
            if (run.Status != RunStatus.Completed)
            {
                throw new Exception("Run did not complete successfully, error: " + run.LastError?.Message);
            }

            // Retrieve all messages from the agent client
            AsyncPageable<PersistentThreadMessage> messages = agentClient.Messages.GetMessagesAsync(
                threadId: thread.Id,
                order: ListSortOrder.Ascending
            );

            var response = string.Empty;
            // Process messages in order
            await foreach (PersistentThreadMessage threadMessage in messages)
            {
                foreach (MessageContent contentItem in threadMessage.ContentItems)
                {
                    if (contentItem is MessageTextContent textItem)
                    {
                        response = textItem.Text;

                        // If we have Text URL citation annotations, reformat the response to show title & URL for citations
                        if (textItem.Annotations != null)
                        {
                            foreach (MessageTextAnnotation annotation in textItem.Annotations)
                            {
                                if (annotation is MessageTextUriCitationAnnotation urlAnnotation)
                                {
                                    response = response.Replace(urlAnnotation.Text, $" [{urlAnnotation.UriCitation.Title}] ({urlAnnotation.UriCitation.Uri})");
                                }
                            }
                        }                        
                    }
                    else if (contentItem is MessageImageFileContent imageFileItem)
                    {
                        Console.Write($"<image from ID: {imageFileItem.FileId}");
                    }
                    Console.WriteLine();
                }
            }

            return response;


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI Foundry agent");
            return $"Error calling AI Foundry agent: {ex.Message}";
        }
    }

    private async Task<string> GetDummyResponseAsync(string query)
    {
        await Task.Delay(500); // Simulate processing time

        var lowerQuery = query.ToLower();

        if (lowerQuery.Contains("knowledge") || lowerQuery.Contains("information"))
        {
            return @"Based on the AI Foundry agent's advanced knowledge base, here's the comprehensive information:

**Enhanced Knowledge Retrieval:**
The AI agent has processed your query using state-of-the-art retrieval-augmented generation (RAG) techniques, accessing a curated knowledge base that includes:

• Domain-specific documents and resources
• Real-time data integration capabilities  
• Contextual understanding and reasoning
• Multi-modal information processing

**Key Insights:**
- Advanced semantic search across enterprise knowledge repositories
- Dynamic content retrieval with relevance scoring
- Intelligent summarization and synthesis of information
- Context-aware response generation tailored to your specific needs

**Technical Capabilities:**
- Vector-based similarity search for precise information retrieval
- Hybrid search combining keyword and semantic matching
- Real-time knowledge base updates and synchronization
- Secure access to enterprise-grade information systems

The AI Foundry agent leverages cutting-edge natural language processing to deliver accurate, relevant, and contextually appropriate responses.";
        }

        if (lowerQuery.Contains("technical") || lowerQuery.Contains("documentation"))
        {
            return @"Technical documentation and implementation guidance from AI Foundry agent:

**System Architecture:**
The AI Foundry agent is deployed on Azure's enterprise AI infrastructure, providing:

• Scalable processing capabilities for complex queries
• Integration with Azure AI services and cognitive APIs
• Secure data handling and compliance with enterprise standards
• Real-time monitoring and performance optimization

**API Integration:**
- RESTful endpoints for seamless integration
- Authentication and authorization mechanisms
- Rate limiting and quota management
- Comprehensive logging and analytics

**Implementation Best Practices:**
- Proper error handling and fallback mechanisms
- Caching strategies for improved performance
- Load balancing and high availability configurations
- Security considerations for enterprise deployment

**Available Features:**
- Natural language query processing
- Document analysis and summarization
- Multi-turn conversation support
- Custom knowledge base integration
- Real-time response generation

For detailed API specifications, authentication requirements, and integration examples, refer to the AI Foundry documentation portal.";
        }

        return @"AI Foundry agent response for your query:

**Intelligent Processing Results:**
The advanced AI agent has analyzed your request using sophisticated natural language understanding and knowledge retrieval techniques.

**Response Highlights:**
• Contextual analysis of your query parameters
• Relevant information extraction from enterprise knowledge base
• Intelligent synthesis and summarization
• Accuracy verification and source attribution

**Agent Capabilities Demonstrated:**
- Multi-domain knowledge access and retrieval
- Complex reasoning and inference capabilities
- Real-time processing with sub-second response times
- Adaptive learning from interaction patterns

**Quality Assurance:**
- Source verification and fact-checking
- Confidence scoring for response reliability
- Continuous improvement through feedback loops
- Enterprise-grade security and privacy protection

The AI Foundry agent combines the power of large language models with domain-specific knowledge to provide accurate, relevant, and actionable insights.

⚠️ *This is a demo response. Configure AI Foundry connection settings (endpoint, API key, and agent name) for live agent responses.*";
    }

}

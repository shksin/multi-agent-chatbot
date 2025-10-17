namespace MultiAgentSystem.Api.Agents;

public interface IOrchestratorAgent
{
    Task<OrchestratorResponse> ProcessQueryAsync(string query, string? authToken = null, CancellationToken cancellationToken = default);
}

public class OrchestratorAgent : IOrchestratorAgent
{
    private readonly IRagAgent _ragAgent;
    private readonly IUserAgent _userAgent;
    private readonly ICustomRAGAgent _customRAGAgent;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        IRagAgent ragAgent,
        IUserAgent userAgent,
        ICustomRAGAgent customRAGAgent,
        IConfiguration configuration,
        ILogger<OrchestratorAgent> logger)
    {
        _ragAgent = ragAgent;
        _userAgent = userAgent;
        _customRAGAgent = customRAGAgent;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OrchestratorResponse> ProcessQueryAsync(
        string query, 
        string? authToken = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Orchestrator processing query: {Query}, Auth Token Present: {HasToken}", 
            query, !string.IsNullOrEmpty(authToken));

        var response = new OrchestratorResponse
        {
            Query = query,
            Timestamp = DateTime.UtcNow,
            HasAuthToken = !string.IsNullOrEmpty(authToken)
        };

        try
        {
            // Get the search configuration to determine which search agent to use
            var searchConfig = _configuration["SearchType"] ?? "SearchIndex";
            bool useCustomRAG = searchConfig.Equals("BingCustom", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogInformation("Search configuration: {SearchConfig}, Using Custom RAG: {UseCustomRAG}", 
                searchConfig, useCustomRAG);
            
            // Priority-based logic: Try User Agent first if authenticated, then use configured search agent
            if (!string.IsNullOrEmpty(authToken))
            {
                _logger.LogInformation("User is authenticated, trying User Agent first");
                
                try
                {
                    response.UserResult = await _userAgent.QueryAsync(query, authToken, cancellationToken);
                    
                    // Check if User Agent can handle this query
                    if (response.UserResult != "USER_AGENT_NO_MATCH")
                    {
                        response.AgentsCalled.Add("User Agent");
                        _logger.LogInformation("User Agent handled the query successfully");
                        
                        // User Agent provided a valid response, no need to call other agents
                        response.RagResult = null;
                        response.CustomRAGResult = null;
                    }
                    else
                    {
                        _logger.LogInformation("User Agent cannot handle this query, using configured search agent");
                        response.UserResult = null;
                        
                        // Use configured search agent
                        if (useCustomRAG)
                        {
                            try
                            {
                                response.CustomRAGResult = await _customRAGAgent.QueryAsync(query, cancellationToken);
                                response.AgentsCalled.Add("Custom RAG Agent");
                                _logger.LogInformation("Custom RAG Agent completed successfully");
                                response.RagResult = null;
                            }
                            catch (Exception customRagEx)
                            {
                                _logger.LogError(customRagEx, "Custom RAG Agent failed");
                                response.CustomRAGResult = null;
                                response.Errors.Add($"Custom RAG Agent: {customRagEx.Message}");
                                response.RagResult = "**Custom RAG Agent Error:** Unable to retrieve information from AI Foundry.";
                            }
                        }
                        else
                        {
                            try
                            {
                                response.RagResult = await _ragAgent.QueryAsync(query, cancellationToken);
                                response.AgentsCalled.Add("RAG Agent");
                                _logger.LogInformation("RAG Agent completed successfully");
                                response.CustomRAGResult = null;
                            }
                            catch (Exception ragEx)
                            {
                                _logger.LogError(ragEx, "RAG Agent failed");
                                response.RagResult = "**RAG Agent Error:** Unable to retrieve knowledge base information.";
                                response.Errors.Add($"RAG Agent: {ragEx.Message}");
                            }
                        }
                    }
                }
                catch (Exception userEx)
                {
                    _logger.LogError(userEx, "User Agent failed, using configured search agent");
                    response.UserResult = null;
                    response.Errors.Add($"User Agent: {userEx.Message}");
                    
                    // Use configured search agent as fallback
                    if (useCustomRAG)
                    {
                        try
                        {
                            response.CustomRAGResult = await _customRAGAgent.QueryAsync(query, cancellationToken);
                            response.AgentsCalled.Add("Custom RAG Agent");
                            _logger.LogInformation("Custom RAG Agent completed successfully as fallback after User Agent error");
                            response.RagResult = null;
                        }
                        catch (Exception customRagEx)
                        {
                            _logger.LogError(customRagEx, "Custom RAG Agent fallback also failed");
                            response.CustomRAGResult = null;
                            response.Errors.Add($"Custom RAG Agent: {customRagEx.Message}");
                            response.RagResult = "**Custom RAG Agent Error:** Unable to retrieve information from AI Foundry.";
                        }
                    }
                    else
                    {
                        try
                        {
                            response.RagResult = await _ragAgent.QueryAsync(query, cancellationToken);
                            response.AgentsCalled.Add("RAG Agent");
                            _logger.LogInformation("RAG Agent completed successfully as fallback after User Agent error");
                            response.CustomRAGResult = null;
                        }
                        catch (Exception ragEx)
                        {
                            _logger.LogError(ragEx, "RAG Agent fallback also failed");
                            response.RagResult = "**RAG Agent Error:** Unable to retrieve knowledge base information.";
                            response.Errors.Add($"RAG Agent: {ragEx.Message}");
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("No auth token provided, using configured search agent: {SearchConfig}", searchConfig);
                
                // No authentication, use configured search agent
                if (useCustomRAG)
                {
                    try
                    {
                        response.CustomRAGResult = await _customRAGAgent.QueryAsync(query, cancellationToken);
                        response.AgentsCalled.Add("Custom RAG Agent");
                        _logger.LogInformation("Custom RAG Agent completed successfully");
                        response.RagResult = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Custom RAG Agent failed");
                        response.CustomRAGResult = null;
                        response.Errors.Add($"Custom RAG Agent: {ex.Message}");
                        response.RagResult = "**Custom RAG Agent Error:** Unable to retrieve information from AI Foundry.";
                    }
                }
                else
                {
                    try
                    {
                        response.RagResult = await _ragAgent.QueryAsync(query, cancellationToken);
                        response.AgentsCalled.Add("RAG Agent");
                        _logger.LogInformation("RAG Agent completed successfully");
                        response.CustomRAGResult = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "RAG Agent failed");
                        response.RagResult = "**RAG Agent Error:** Unable to retrieve knowledge base information.";
                        response.Errors.Add($"RAG Agent: {ex.Message}");
                    }
                }
                
                response.UserResult = null;
            }

            // Synthesize the final response
            response.SynthesizedResponse = SynthesizeResponse(response);
            response.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator failed to process query");
            response.Success = false;
            response.Errors.Add($"Orchestrator: {ex.Message}");
            response.SynthesizedResponse = "I apologize, but I encountered an error processing your request. Please try again.";
        }

        return response;
    }

    private string SynthesizeResponse(OrchestratorResponse response)
    {
        var parts = new List<string>();

        // Determine which agent provided the primary response
        bool userAgentProvided = !string.IsNullOrEmpty(response.UserResult);
        bool ragAgentProvided = !string.IsNullOrEmpty(response.RagResult);
        bool customRAGProvided = !string.IsNullOrEmpty(response.CustomRAGResult);

        // Add the main response (priority: User > Custom RAG > Standard RAG)
        if (userAgentProvided)
        {
            // User Agent provided the response
            parts.Add(response.UserResult!);
        }
        else if (customRAGProvided)
        {
            // Custom RAG Agent provided the response
            parts.Add(response.CustomRAGResult!);
        }
        else if (ragAgentProvided)
        {
            // Standard RAG Agent provided the response
            parts.Add(response.RagResult!);
        }
        else
        {
            // Fallback message if no agent provided a response
            parts.Add("I apologize, but I couldn't retrieve information for your query at this time. Please try again or contact support for assistance.");
        }

        // Add footer with agent information
        if (response.AgentsCalled.Count > 0)
        {
            parts.Add("");
            parts.Add("---");
            parts.Add("");
            
            if (response.HasAuthToken && userAgentProvided)
            {
                parts.Add($"*Personalized response from your banking data*");
            }
            else if (customRAGProvided)
            {
                parts.Add($"*Response from AI Foundry agent (Search: BingCustom)*");
            }
            else if (ragAgentProvided)
            {
                if (response.HasAuthToken)
                {
                    parts.Add($"*General information from search index (no personal data available for this query)*");
                }
                else
                {
                    parts.Add($"*Information from search index (Search: SearchIndex)*");
                }
            }
            else
            {
                parts.Add($"*Information from configured search provider*");
            }
            
            parts.Add($"*Generated by: {string.Join(", ", response.AgentsCalled)}*");
        }
        
        if (response.Errors.Count > 0)
        {
            parts.Add("");
            parts.Add("⚠️ *Some information could not be retrieved due to technical issues.*");
        }

        return string.Join("\n", parts);
    }
}

public class OrchestratorResponse
{
    public string Query { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool HasAuthToken { get; set; }
    public List<string> AgentsCalled { get; set; } = new();
    public string? RagResult { get; set; }
    public string? UserResult { get; set; }
    public string? CustomRAGResult { get; set; }
    public string SynthesizedResponse { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}

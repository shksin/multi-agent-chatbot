namespace MultiAgentSystem.Api.Agents;

using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using OpenAI.Chat;
using System.Text.Json;

public interface IOrchestratorAgent
{
    Task<OrchestratorResponse> ProcessQueryAsync(string query, string? authToken = null, CancellationToken cancellationToken = default);
}

public class OrchestratorAgent : IOrchestratorAgent
{
    private readonly IAISearchAgent _aiSearchAgent;
    private readonly IUserAgent _userAgent;
    private readonly IBingCustomSearchAgent _bingCustomSearchAgent;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrchestratorAgent> _logger;
    private readonly ChatClient? _chatClient;
    private readonly bool _summarizationEnabled;
    private readonly string? _deploymentName;
    private readonly int _maxTokens;
    private readonly float _temperature;

    public OrchestratorAgent(
        IAISearchAgent aiSearchAgent,
        IUserAgent userAgent,
        IBingCustomSearchAgent bingCustomSearchAgent,
        IConfiguration configuration,
        ILogger<OrchestratorAgent> logger)
    {
        _aiSearchAgent = aiSearchAgent;
        _userAgent = userAgent;
        _bingCustomSearchAgent = bingCustomSearchAgent;
        _configuration = configuration;
        _logger = logger;

        // Initialize Azure OpenAI client for summarization
        _summarizationEnabled = _configuration.GetValue<bool>("AzureOpenAI:EnableSummarization");
        
        if (_summarizationEnabled)
        {
            var endpoint = _configuration["AzureOpenAI:Endpoint"];
            _deploymentName = _configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4.1";
            _maxTokens = _configuration.GetValue<int>("AzureOpenAI:MaxTokens", 800);
            _temperature = _configuration.GetValue<float>("AzureOpenAI:Temperature", 0.7f);

            if (!string.IsNullOrEmpty(endpoint))
            {
                try
                {
                    var azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());

                    // Initialize the ChatClient with the specified deployment name
                    _chatClient = azureClient.GetChatClient(_deploymentName);
                    _logger.LogInformation("Azure OpenAI summarization enabled with deployment: {DeploymentName}", _deploymentName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Azure OpenAI client. Summarization will be disabled.");
                    _summarizationEnabled = false;
                }
            }
            else
            {
                _logger.LogWarning("Azure OpenAI endpoint not configured. Summarization will be disabled.");
                _summarizationEnabled = false;
            }
        }
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
            var searchConfig = _configuration["SearchType"] ?? "AISearch";
            bool useCustomRAG = searchConfig.Equals("BingCustomSearch", StringComparison.OrdinalIgnoreCase);
            
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
                                response.CustomRAGResult = await _bingCustomSearchAgent.QueryAsync(query, cancellationToken);
                                response.AgentsCalled.Add("Bing Custom Search Agent");
                                _logger.LogInformation("Bing Custom Search Agent completed successfully");
                                response.RagResult = null;
                            }
                            catch (Exception customRagEx)
                            {
                                _logger.LogError(customRagEx, "Bing Custom Search Agent failed");
                                response.CustomRAGResult = null;
                                response.Errors.Add($"Bing Custom Search Agent: {customRagEx.Message}");
                                response.RagResult = "**Bing Custom Search Agent Error:** Unable to retrieve information from AI Foundry.";
                            }
                        }
                        else
                        {
                            try
                            {
                                response.RagResult = await _aiSearchAgent.QueryAsync(query, cancellationToken);
                                response.AgentsCalled.Add("AI Search Agent");
                                _logger.LogInformation("AI Search Agent completed successfully");
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
                            response.CustomRAGResult = await _bingCustomSearchAgent.QueryAsync(query, cancellationToken);
                            response.AgentsCalled.Add("Bing Custom Search Agent");
                            _logger.LogInformation("Bing Custom Search Agent completed successfully as fallback after User Agent error");
                            response.RagResult = null;
                        }
                        catch (Exception customRagEx)
                        {
                            _logger.LogError(customRagEx, "Bing Custom Search Agent fallback also failed");
                            response.CustomRAGResult = null;
                            response.Errors.Add($"Bing Custom Search Agent: {customRagEx.Message}");
                            response.RagResult = "**Bing Custom Search Agent Error:** Unable to retrieve information from AI Foundry.";
                        }
                    }
                    else
                    {
                        try
                        {
                            response.RagResult = await _aiSearchAgent.QueryAsync(query, cancellationToken);
                            response.AgentsCalled.Add("AI Search Agent");
                            _logger.LogInformation("AI Search Agent completed successfully as fallback after User Agent error");
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
                        response.CustomRAGResult = await _bingCustomSearchAgent.QueryAsync(query, cancellationToken);
                        response.AgentsCalled.Add("Bing Custom Search Agent");
                        _logger.LogInformation("Bing Custom Search Agent completed successfully");
                        response.RagResult = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Bing Custom Search Agent failed");
                        response.CustomRAGResult = null;
                        response.Errors.Add($"Bing Custom Search Agent: {ex.Message}");
                        response.RagResult = "**Bing Custom Search Agent Error:** Unable to retrieve information from AI Foundry.";
                    }
                }
                else
                {
                    try
                    {
                        response.RagResult = await _aiSearchAgent.QueryAsync(query, cancellationToken);
                        response.AgentsCalled.Add("AI Search Agent");
                        _logger.LogInformation("AI Search Agent completed successfully");
                        response.CustomRAGResult = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AI Search Agent failed");
                        response.RagResult = "**AI Search Agent Error:** Unable to retrieve knowledge base information.";
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

        string rawResponse = string.Empty;
        string agentType = string.Empty;

        // Get the raw response from the agent
        if (userAgentProvided)
        {
            rawResponse = response.UserResult!;
            agentType = "User Agent";
        }
        else if (customRAGProvided)
        {
            rawResponse = response.CustomRAGResult!;
            agentType = "Bing Custom Search Agent";
        }
        else if (ragAgentProvided)
        {
            rawResponse = response.RagResult!;
            agentType = "AI Search Agent";
        }
        else
        {
            // Fallback message if no agent provided a response
            parts.Add("I apologize, but I couldn't retrieve information for your query at this time. Please try again or contact support for assistance.");
            return string.Join("\n", parts);
        }

        // Apply Azure OpenAI summarization if enabled
        string finalResponse = rawResponse;
        if (_summarizationEnabled && _chatClient != null && !string.IsNullOrEmpty(rawResponse))
        {
            try
            {
                finalResponse = SummarizeWithAzureOpenAI(response.Query, rawResponse, agentType).GetAwaiter().GetResult();
                _logger.LogInformation("Successfully summarized response using Azure OpenAI");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to summarize response with Azure OpenAI, using original response");
                finalResponse = rawResponse;
            }
        }

        parts.Add(finalResponse);
               
        
        if (response.Errors.Count > 0)
        {
            parts.Add("");
            parts.Add("⚠️ *Some information could not be retrieved due to technical issues.*");
        }

        return string.Join("\n", parts);
    }

    private async Task<string> SummarizeWithAzureOpenAI(string userQuery, string rawResponse, string agentType)
    {
        if (_chatClient == null || string.IsNullOrEmpty(_deploymentName))
        {
            return rawResponse;
        }

        var systemPrompt = @"You are a helpful AI assistant for NAB (National Australia Bank). 
Your role is to take raw data or search results and provide a concise, well-formatted summary.

Critical Guidelines:
- Provide EXACTLY 3 sentences maximum - no more, no less
- Use clear, natural, conversational language
- Focus on the most important information that directly answers the user's question
- Use proper markdown formatting:
  * Use **bold** for key information (amounts, dates, account names)
  * Use bullet points (•) for lists
  * Use line breaks for better readability between different topics
  * Use proper spacing and paragraph structure
- Maintain complete accuracy - don't add information not present in the source data
- If the data contains banking information (balances, transactions, etc.), present key numbers prominently with proper formatting
- Keep the tone professional but friendly
- Don't mention you're summarizing - just provide the answer directly
- Ensure proper spacing and structure for easy reading";

        var userPrompt = $@"User Question: {userQuery}

Source Data from {agentType}:
{rawResponse}

Provide a concise, well-formatted summary in EXACTLY 3 sentences that directly answers the user's question. 
Use markdown formatting with bold text for emphasis on key information (amounts, names, dates). 
Ensure proper spacing and structure for maximum readability.";

        try
        {
            _logger.LogDebug("Sending summarization request to Azure OpenAI with model: {Model}", _deploymentName);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = _temperature, // Lower temperature for more focused, concise responses
                MaxOutputTokenCount = _maxTokens, // Reduced to ensure brevity (3 sentences)
                TopP = (float)0.95,
                FrequencyPenalty = (float)0.3, // Encourage variety in word choice
                PresencePenalty = (float)0.2 // Reduce repetition
            };

            // Create the chat completion request
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options);

            // Extract the actual text content from the response
            if (completion != null && completion.Content != null && completion.Content.Count > 0)
            {
                var summarizedText = completion.Content[0].Text;
                
                if (!string.IsNullOrWhiteSpace(summarizedText))
                {
                    // Post-process for better formatting
                    summarizedText = FormatSummaryText(summarizedText);
                    
                    _logger.LogDebug("Successfully summarized response with Azure OpenAI");
                    return summarizedText;
                }
                else
                {
                    _logger.LogWarning("Azure OpenAI returned empty content, using original");
                    return rawResponse;
                }
            }
            else
            {
                _logger.LogWarning("Azure OpenAI returned null or empty response, using original");
                return rawResponse;
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure OpenAI API request failed: {Message}", ex.Message);
            return rawResponse; // Fallback to original response on error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Azure OpenAI summarization");
            return rawResponse; // Fallback to original response on error
        }
    }

    private string FormatSummaryText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Normalize line breaks
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // Ensure proper spacing after periods followed by capital letters (sentence breaks)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\.([A-Z])", ". $1");
        
        // Remove excessive whitespace while preserving intentional line breaks
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line));
        
        text = string.Join("\n\n", lines);
        
        // Ensure there's spacing around bullet points
        text = System.Text.RegularExpressions.Regex.Replace(text, @"([^\n])(\s*[•\-\*]\s)", "$1\n$2");
        
        return text.Trim();
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

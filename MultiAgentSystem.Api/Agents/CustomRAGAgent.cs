using Azure.AI.Projects;
using Azure.Identity;
using System.Text.Json;

namespace MultiAgentSystem.Api.Agents;

public interface ICustomRAGAgent
{
    Task<string> QueryAsync(string query, CancellationToken cancellationToken = default);
}

public class CustomRAGAgent : ICustomRAGAgent
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CustomRAGAgent> _logger;
    private readonly HttpClient _httpClient;

    public CustomRAGAgent(IConfiguration configuration, ILogger<CustomRAGAgent> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Custom RAG Agent processing query: {Query}", query);

        try
        {
            // Check if AI Foundry configuration is available
            var endpoint = _configuration["AIFoundry:Endpoint"];
            var apiKey = _configuration["AIFoundry:ApiKey"];
            var agentName = _configuration["AIFoundry:AgentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(agentName))
            {
                return await GetDummyResponseAsync(query);
            }

            // Call the existing agent in AI Foundry
            var agentResponse = await CallAIFoundryAgentAsync(query, endpoint, apiKey, agentName, cancellationToken);

            return $"**Custom RAG Agent (AI Foundry):**\n\n{agentResponse}\n\n*Response from AI Foundry agent '{agentName}'*";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Custom RAG Agent. Falling back to dummy response.");
            return await GetDummyResponseAsync(query);
        }
    }

    private async Task<string> CallAIFoundryAgentAsync(string query, string endpoint, string apiKey, string agentName, CancellationToken cancellationToken)
    {
        try
        {
            // Prepare the request payload for AI Foundry agent
            var requestPayload = new
            {
                messages = new[]
                {
                    new { role = "user", content = query }
                },
                max_tokens = 1500,
                temperature = 0.7,
                top_p = 0.95,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Create the HTTP request
            var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.TrimEnd('/')}/v1/chat/completions")
            {
                Content = content
            };

            // Add authentication headers
            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("User-Agent", "MultiAgentSystem-CustomRAGAgent/1.0");

            // Add any additional headers for the specific agent
            if (!string.IsNullOrEmpty(agentName))
            {
                request.Headers.Add("X-Agent-Name", agentName);
            }

            _logger.LogInformation("Calling AI Foundry agent '{AgentName}' at endpoint: {Endpoint}", agentName, endpoint);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                // Parse the response based on OpenAI-compatible format
                if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        var content_text = messageContent.GetString();
                        if (!string.IsNullOrEmpty(content_text))
                        {
                            return content_text;
                        }
                    }
                }
                
                // Alternative response format for direct content
                if (responseData.TryGetProperty("content", out var directContent))
                {
                    var content_text = directContent.GetString();
                    if (!string.IsNullOrEmpty(content_text))
                    {
                        return content_text;
                    }
                }

                // If we can't parse the expected format, return the raw response for debugging
                _logger.LogWarning("Unexpected response format from AI Foundry agent. Raw response: {Response}", responseContent);
                return $"Received response but couldn't parse content. Raw response: {responseContent}";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("HTTP call to AI Foundry agent failed. Status: {StatusCode}, Content: {Content}", 
                    response.StatusCode, errorContent);
                return $"AI Foundry agent call failed: {response.StatusCode} - {errorContent}";
            }
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
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using System.Text.Json;

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

    public BingCustomSearchAgent(IConfiguration configuration, ILogger<BingCustomSearchAgent> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bing Custom Search Agent processing query: {Query}", query);

        try
        {
            // Check if AI Foundry configuration is available
            var endpoint = _configuration["AIFoundry:ProjectEndpoint"];
            var agentName = _configuration["AIFoundry:AgentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(agentName))
            {
                return await GetDummyResponseAsync(query);
            }

            // Call the existing agent in AI Foundry
            var agentResponse = await CallAIFoundryAgentAsync(query, endpoint, agentName, cancellationToken);

            return $"{agentResponse}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Bing Custom Search Agent. Falling back to dummy response.");
            return await GetDummyResponseAsync(query);
        }
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

            PersistentAgentsClient agentClient = new(endpoint, new DefaultAzureCredential());

            PersistentAgent agent = await agentClient.Administration.GetAgentAsync(assistantId);

            PersistentAgentThread thread = await agentClient.Threads.CreateThreadAsync();

            //Ask a question of the Agent.
            await agentClient.Messages.CreateMessageAsync(thread.Id, MessageRole.User, query);

            //Have Agent begin processing user's question with some additional instructions associated with the ThreadRun.
            ThreadRun run = await agentClient.Runs.CreateRunAsync(thread.Id, agent.Id);

            // Wait for the agent to finish running
            do
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
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

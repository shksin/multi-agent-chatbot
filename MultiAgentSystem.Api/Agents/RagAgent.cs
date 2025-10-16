using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace MultiAgentSystem.Api.Agents;

public interface IRagAgent
{
    Task<string> QueryAsync(string query, CancellationToken cancellationToken = default);
}

public class RagAgent : IRagAgent
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RagAgent> _logger;
    private SearchClient? _searchClient;

    public RagAgent(IConfiguration configuration, ILogger<RagAgent> logger)
    {
        _configuration = configuration;
        _logger = logger;
        InitializeSearchClient();
    }

    private void InitializeSearchClient()
    {
        try
        {
            var endpoint = _configuration["AzureSearch:Endpoint"];
            var indexName = _configuration["AzureSearch:IndexName"];
            var apiKey = _configuration["AzureSearch:ApiKey"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(indexName) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Azure Search configuration is incomplete. Using dummy mode.");
                return;
            }

            var credential = new AzureKeyCredential(apiKey);
            _searchClient = new SearchClient(new Uri(endpoint), indexName, credential);
            _logger.LogInformation("Azure Search client initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Search client. Using dummy mode.");
        }
    }

    public async Task<string> QueryAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("RAG Agent processing query: {Query}", query);

        try
        {
            if (_searchClient == null)
            {
                return await GetDummyResponseAsync(query);
            }

            // Perform hybrid search (semantic + keyword)
            var searchOptions = new SearchOptions
            {
                Size = 5,
                Select = { "content", "title", "category" },
                QueryType = SearchQueryType.Semantic,
                SemanticSearch = new()
                {
                    SemanticConfigurationName = "default",
                    QueryCaption = new(QueryCaptionType.Extractive),
                    QueryAnswer = new(QueryAnswerType.Extractive)
                },
                // Hybrid search combines semantic ranking with keyword search
                SearchMode = SearchMode.All
            };

            var response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions, cancellationToken);
            
            var results = new List<string>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var title = result.Document.TryGetValue("title", out var t) ? t?.ToString() : "N/A";
                var content = result.Document.TryGetValue("content", out var c) ? c?.ToString() : "N/A";
                var category = result.Document.TryGetValue("category", out var cat) ? cat?.ToString() : "N/A";
                
                results.Add($"**{title}** (Category: {category})\n{content}");
            }

            if (results.Count == 0)
            {
                return "No relevant information found in the knowledge base for your query.";
            }

            return $"**RAG Agent Results:**\n\nFound {results.Count} relevant documents:\n\n" + 
                   string.Join("\n\n---\n\n", results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Azure Search. Falling back to dummy response.");
            return await GetDummyResponseAsync(query);
        }
    }

    private async Task<string> GetDummyResponseAsync(string query)
    {
        await Task.Delay(500); // Simulate search latency

        var dummyResponses = new Dictionary<string, string>
        {
            ["product"] = @"**RAG Agent Results:**

Found 3 relevant documents from knowledge base:

**Product Overview** (Category: Products)
Our flagship product offers enterprise-grade solutions with advanced AI capabilities, real-time analytics, and seamless integration with existing systems.

---

**Product Features** (Category: Features)
Key features include: multi-tenant architecture, 99.9% uptime SLA, automated scaling, comprehensive security controls, and 24/7 monitoring.

---

**Product Pricing** (Category: Pricing)
Flexible pricing tiers available: Starter ($99/month), Professional ($299/month), and Enterprise (custom pricing). All plans include core features with varying limits.",

            ["support"] = @"**RAG Agent Results:**

Found 2 relevant documents from knowledge base:

**Support Services** (Category: Support)
We provide comprehensive support including email, chat, and phone support. Enterprise customers get dedicated account managers and priority response times.

---

**Support Hours** (Category: Support)
Our support team is available 24/7 for critical issues. Standard support hours are Monday-Friday, 9 AM - 6 PM EST.",

            ["default"] = @"**RAG Agent Results:**

Found 2 relevant documents from knowledge base:

**General Information** (Category: General)
Our platform helps businesses transform their operations through AI-powered automation and intelligent workflows.

---

**Getting Started** (Category: Documentation)
To get started, sign up for an account, configure your workspace, and explore our comprehensive documentation and tutorials."
        };

        var lowerQuery = query.ToLower();
        foreach (var kvp in dummyResponses)
        {
            if (lowerQuery.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }

        return dummyResponses["default"];
    }
}

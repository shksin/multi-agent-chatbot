# Configuration Examples

## Example 1: Using Azure Search Index (Default)

```json
{
  "Search": "SearchIndex",
  "AzureSearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "IndexName": "your-index-name",
    "ApiKey": "your-api-key"
  }
}
```

**Behavior:**
- Non-authenticated queries → RagAgent → Azure Search Index
- Authenticated queries → UserAgent (if applicable) → RagAgent (fallback)

## Example 2: Using AI Foundry Agent

```json
{
  "Search": "BingCustom",
  "AIFoundry": {
    "Endpoint": "https://your-ai-foundry-endpoint.azurewebsites.net",
    "ApiKey": "your-ai-foundry-api-key",
    "AgentName": "your-existing-agent-name"
  }
}
```

**Behavior:**
- Non-authenticated queries → CustomRAGAgent → AI Foundry Agent
- Authenticated queries → UserAgent (if applicable) → CustomRAGAgent (fallback)

## Testing the Configuration

### Test Search Index Mode:
1. Set `"Search": "SearchIndex"` in appsettings.json
2. Restart the application
3. Make a query - response footer should show: `*Information from search index (Search: SearchIndex)*`

### Test AI Foundry Mode:
1. Set `"Search": "BingCustom"` in appsettings.json
2. Restart the application  
3. Make a query - response footer should show: `*Response from AI Foundry agent (Search: BingCustom)*`

### Test User Agent Priority:
1. Use any Search configuration
2. Make an authenticated request with personal data query (e.g., "account balance")
3. Response footer should show: `*Personalized response from your banking data*`
4. Make an authenticated request with general query (e.g., "what is AI?")
5. Response footer should show the configured search provider
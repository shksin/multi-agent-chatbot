# Search Configuration Guide

## Overview

The Multi-Agent System now supports configuration-based search agent selection through the `Search` configuration setting. This allows you to choose between different search providers without code changes.

## Configuration

Add the `Search` setting to your `appsettings.json`:

```json
{
  "Search": "SearchIndex",  // or "BingCustom"
  "AzureSearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "IndexName": "your-index-name",
    "ApiKey": "your-api-key"
  },
  "AIFoundry": {
    "Endpoint": "https://your-ai-foundry-endpoint.azurewebsites.net",
    "ApiKey": "your-ai-foundry-api-key",
    "AgentName": "your-existing-agent-name"
  }
}
```

## Search Options

### SearchIndex
- **Value**: `"SearchIndex"`
- **Agent Used**: RagAgent
- **Data Source**: Azure Search Index
- **Best For**: General knowledge base queries, document search, enterprise content

### BingCustom
- **Value**: `"BingCustom"`
- **Agent Used**: CustomRAGAgent
- **Data Source**: AI Foundry Agent
- **Best For**: Advanced AI processing, complex analysis, specialized domain knowledge

## Agent Priority Logic

The orchestrator follows this priority sequence:

```
1. User Authentication Check
   ├── Authenticated User
   │   ├── UserAgent (personal data queries)
   │   └── If UserAgent can't handle → Configured Search Agent
   └── Non-Authenticated User
       └── Configured Search Agent (based on Search setting)

2. Search Agent Selection (based on "Search" config)
   ├── "SearchIndex" → RagAgent (Azure Search)
   └── "BingCustom" → CustomRAGAgent (AI Foundry)
```

## Example Scenarios

### Scenario 1: General Enterprise Search
```json
{
  "Search": "SearchIndex"
}
```
- Non-authenticated users get responses from Azure Search Index
- Authenticated users get personal data from UserAgent, fallback to Azure Search

### Scenario 2: AI-Powered Search
```json
{
  "Search": "BingCustom"
}
```
- Non-authenticated users get responses from AI Foundry Agent
- Authenticated users get personal data from UserAgent, fallback to AI Foundry Agent

## Response Indicators

The system indicates which search provider was used in the response footer:

- **SearchIndex**: `*Information from search index (Search: SearchIndex)*`
- **BingCustom**: `*Response from AI Foundry agent (Search: BingCustom)*`
- **UserAgent**: `*Personalized response from your banking data*`

## Error Handling

- If the configured search agent fails, an appropriate error message is returned
- No automatic fallback between search providers (maintains consistency)
- Detailed error logging for troubleshooting

## Configuration Changes

To switch between search providers:

1. Update the `Search` value in `appsettings.json`
2. Restart the application
3. All new queries will use the updated search provider

No code changes required!
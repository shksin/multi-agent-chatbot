using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MultiAgentSystem.Api.Models;

namespace MultiAgentSystem.Api.Agents;

public interface IUserAgent
{
    Task<string> QueryAsync(string query, string authToken, CancellationToken cancellationToken = default);
}

public class UserAgent : IUserAgent
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserAgent> _logger;
    private readonly HttpClient _httpClient;
    private static readonly Dictionary<string, (string Response, DateTime Expiry)> _cache = new();
    private static readonly object _cacheLock = new object();

    public UserAgent(IConfiguration configuration, ILogger<UserAgent> logger, HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configure HttpClient if not already configured
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://localhost:58550");
        }
        
        var timeoutSeconds = _configuration.GetValue<int>("HttpClient:TimeoutSeconds", 30);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<string> QueryAsync(string query, string authToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User Agent processing query: {Query}", query);

        try
        {
            var lowerQuery = query.ToLower();
            
            // Check if this is a user-specific query that UserAgent can handle
            if (!IsUserSpecificQuery(lowerQuery))
            {
                _logger.LogInformation("Query '{Query}' is not user-specific, returning no match", query);
                return "USER_AGENT_NO_MATCH";
            }
            
            // Determine which API endpoint to call based on the query
            if (lowerQuery.Contains("profile") || lowerQuery.Contains("personal") || lowerQuery.Contains("who am i") || lowerQuery.Contains("my info"))
            {
                return await CallMockApiAsync("/api/mock-user/profile", authToken, FormatProfileResponse);
            }
            else if (lowerQuery.Contains("account") || lowerQuery.Contains("balance") || lowerQuery.Contains("checking") || lowerQuery.Contains("savings"))
            {
                return await CallMockApiAsync("/api/mock-user/accounts", authToken, FormatAccountsResponse);
            }
            else if (lowerQuery.Contains("transaction") || lowerQuery.Contains("payment") || lowerQuery.Contains("deposit") || lowerQuery.Contains("history"))
            {
                return await CallMockApiAsync("/api/mock-user/transactions?limit=5", authToken, FormatTransactionsResponse);
            }
            else if (lowerQuery.Contains("card") || lowerQuery.Contains("credit") || lowerQuery.Contains("debit"))
            {
                return await CallMockApiAsync("/api/mock-user/cards", authToken, FormatCardsResponse);
            }
            else if (lowerQuery.Contains("loan") || lowerQuery.Contains("mortgage") || lowerQuery.Contains("debt"))
            {
                return await CallMockApiAsync("/api/mock-user/loans", authToken, FormatLoansResponse);
            }
            else if (lowerQuery.Contains("investment") || lowerQuery.Contains("portfolio") || lowerQuery.Contains("stock") || lowerQuery.Contains("401k"))
            {
                return await CallMockApiAsync("/api/mock-user/investments", authToken, FormatInvestmentsResponse);
            }
            else
            {
                // For user-specific queries that don't match specific endpoints, provide account summary
                return await CallMockApiAsync("/api/mock-user/accounts", authToken, FormatAccountSummaryResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in User Agent query processing");
            return "USER_AGENT_NO_MATCH";
        }
    }

    private bool IsUserSpecificQuery(string lowerQuery)
    {
        // Define keywords that indicate user-specific banking queries
        var userSpecificKeywords = new[]
        {
            "my", "mine", "i", "account", "balance", "profile", "personal", "transaction", "payment", 
            "deposit", "history", "card", "credit", "debit", "loan", "mortgage", "debt", "investment", 
            "portfolio", "stock", "401k", "checking", "savings", "who am i", "my info"
        };
        
        // Check if query contains any user-specific keywords
        return userSpecificKeywords.Any(keyword => lowerQuery.Contains(keyword));
    }

    private string ExtractUserNameFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            return userNameClaim?.Value ?? "Unknown User";
        }
        catch
        {
            return "Unknown User";
        }
    }

    private async Task<string> CallMockApiAsync(string endpoint, string authToken, Func<string, string> formatter)
    {
        try
        {
            // Create cache key from endpoint and auth token (last 8 chars for security)
            var cacheKey = $"{endpoint}_{authToken.Substring(Math.Max(0, authToken.Length - 8))}";
            
            // Check cache first (5-minute expiry)
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
                {
                    _logger.LogDebug("Returning cached response for endpoint: {Endpoint}", endpoint);
                    return cached.Response;
                }
            }
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            
            var response = await _httpClient.GetStringAsync(endpoint);
            var formattedResponse = formatter(response);
            
            // Cache the formatted response
            lock (_cacheLock)
            {
                _cache[cacheKey] = (formattedResponse, DateTime.UtcNow.AddMinutes(5));
                
                // Clean up expired entries (simple cleanup)
                var expiredKeys = _cache.Where(kvp => kvp.Value.Expiry <= DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
                foreach (var key in expiredKeys)
                {
                    _cache.Remove(key);
                }
            }
            
            return formattedResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling mock API endpoint: {Endpoint}", endpoint);
            return "USER_AGENT_NO_MATCH";
        }
    }

    private string FormatProfileResponse(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var profile = System.Text.Json.JsonSerializer.Deserialize<ProfileResponse>(jsonData, options);
            if (profile == null) return "**User Agent Error:** No profile data available.";
            
            return $@"**User Profile Information:**

**Personal Details:**
- Name: {profile.FirstName} {profile.LastName}
- Customer ID: {profile.CustomerId}
- Email: {profile.Email}
- Phone: {profile.Phone}
- Member Since: {DateTime.Parse(profile.MemberSince):MMMM dd, yyyy}
- Customer Type: {profile.CustomerType}

**Address:**
- {profile.Address.Street}
- {profile.Address.City}, {profile.Address.State} {profile.Address.ZipCode}
- {profile.Address.Country}

**Banking Details:**
- Relationship Manager: {profile.RelationshipManager}
- Preferred Branch: {profile.PreferredBranch}
- Last Login: {profile.LastLogin:MMMM dd, yyyy 'at' HH:mm}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting profile response. JSON Data: {JsonData}", jsonData);
            return "**User Agent Error:** Unable to format profile information.";
        }
    }

    private string FormatAccountsResponse(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var accounts = System.Text.Json.JsonSerializer.Deserialize<AccountsResponse>(jsonData, options);
            if (accounts?.Accounts == null) return "**User Agent Error:** No account data available.";
            
            var accountInfos = new List<string>();
            foreach (var acc in accounts.Accounts)
            {
                accountInfos.Add($"**{acc.AccountName}** ({acc.AccountType})\n" +
                    $"- Account: {acc.AccountNumber}\n" +
                    $"- Balance: ${acc.Balance:N2} {acc.Currency}\n" +
                    $"- Status: {acc.Status}\n" +
                    $"- Interest Rate: {acc.InterestRate:F2}%\n" +
                    $"- Opened: {DateTime.Parse(acc.OpenedDate):MMMM dd, yyyy}");
            }
            
            var accountInfo = string.Join("\n\n", accountInfos);
            var totalBalance = accounts.Accounts.Sum(acc => acc.Balance);
            
            return $@"**Your Bank Accounts:**

{accountInfo}

**Total Balance Across All Accounts:** ${totalBalance:N2} USD";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting accounts response. JSON Data: {JsonData}", jsonData);
            return "**User Agent Error:** Unable to format accounts information.";
        }
    }

    private string FormatAccountSummaryResponse(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var accounts = System.Text.Json.JsonSerializer.Deserialize<AccountsResponse>(jsonData, options);
            if (accounts?.Accounts == null) return "**User Agent Error:** No account data available.";
            
            var totalBalance = accounts.Accounts.Sum(acc => acc.Balance);
            var accountSummaries = new List<string>();
            foreach (var acc in accounts.Accounts)
            {
                accountSummaries.Add($"- {acc.AccountName}: ${acc.Balance:N2}");
            }
            var accountSummary = string.Join("\n", accountSummaries);
            
            return $@"**Account Summary:**

{accountSummary}

**Total Balance:** ${totalBalance:N2} USD
**Number of Accounts:** {accounts.Accounts.Length}

💡 *Ask me about your transactions, cards, loans, or investments for more details!*";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting account summary response. JSON Data: {JsonData}", jsonData);
            return "**User Agent Error:** Unable to format account summary.";
        }
    }

    private string FormatTransactionsResponse(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var transactions = System.Text.Json.JsonSerializer.Deserialize<TransactionsResponse>(jsonData, options);
            if (transactions?.Transactions == null) return "**User Agent Error:** No transaction data available.";
            
            var transactionInfos = new List<string>();
            foreach (var txn in transactions.Transactions)
            {
                transactionInfos.Add($"**{txn.Description}**\n" +
                    $"- Date: {txn.Date:MMM dd, yyyy}\n" +
                    $"- Amount: {(txn.Amount >= 0 ? "+" : "")}{txn.Amount:C}\n" +
                    $"- Type: {txn.Type}\n" +
                    $"- Category: {txn.Category}\n" +
                    $"- Account: {txn.AccountNumber}");
            }
            
            var transactionInfo = string.Join("\n\n", transactionInfos);
            
            return $@"**Recent Transactions:**

{transactionInfo}

💡 *These are your most recent transactions. Would you like to see more or filter by category?*";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting transactions response. JSON Data: {JsonData}", jsonData);
            return "**User Agent Error:** Unable to format transactions information.";
        }
    }

    private string FormatCardsResponse(string jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var cards = System.Text.Json.JsonSerializer.Deserialize<CardsResponse>(jsonData, options);
            if (cards?.Cards == null) return "**User Agent Error:** No card data available.";
            
            var cardInfos = new List<string>();
            foreach (var card in cards.Cards)
            {
                var cardDetails = $"**{card.CardName}** ({card.CardType})\n" +
                    $"- Card: {card.CardNumber}\n" +
                    $"- Status: {card.Status}\n" +
                    $"- Expires: {card.ExpiryDate}\n";
                
                if (card.CardType == "Credit")
                {
                    cardDetails += $"- Credit Limit: ${card.CreditLimit:N2}\n" +
                        $"- Available Credit: ${card.AvailableCredit:N2}\n" +
                        $"- Current Balance: ${card.CurrentBalance:N2}\n" +
                        $"- Rewards Points: {card.RewardsPoints:N0}";
                }
                else
                {
                    cardDetails += "- Debit card linked to checking account";
                }
                
                cardInfos.Add(cardDetails);
            }
            
            var cardInfo = string.Join("\n\n", cardInfos);
            
            return $@"**Your Cards:**

{cardInfo}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting cards response. JSON Data: {JsonData}", jsonData);
            return "**User Agent Error:** Unable to format cards information.";
        }
    }

    private string FormatLoansResponse(string jsonData)
    {
        try
        {
            var loans = System.Text.Json.JsonSerializer.Deserialize<LoansResponse>(jsonData);
            if (loans?.Loans == null) return "**User Agent Error:** No loan data available.";
            
            if (loans.Loans.Length == 0)
            {
                return "**Your Loans:** You currently have no active loans with us.";
            }
            
            var loanInfos = new List<string>();
            foreach (var loan in loans.Loans)
            {
                loanInfos.Add($"**{loan.LoanType} Loan** ({loan.LoanNumber})\n" +
                    $"- Current Balance: ${loan.CurrentBalance:N2}\n" +
                    $"- Original Amount: ${loan.OriginalAmount:N2}\n" +
                    $"- Interest Rate: {loan.InterestRate:F2}%\n" +
                    $"- Monthly Payment: ${loan.MonthlyPayment:N2}\n" +
                    $"- Next Payment Due: {loan.NextPaymentDate:MMM dd, yyyy}\n" +
                    $"- Remaining Months: {loan.RemainingMonths}");
            }
            
            var loanInfo = string.Join("\n\n", loanInfos);
            var totalBalance = loans.Loans.Sum(loan => loan.CurrentBalance);
            var totalMonthlyPayment = loans.Loans.Sum(loan => loan.MonthlyPayment);
            
            return $@"**Your Loans:**

{loanInfo}

**Total Outstanding Balance:** ${totalBalance:N2}
**Total Monthly Payments:** ${totalMonthlyPayment:N2}";
        }
        catch
        {
            return "**User Agent Error:** Unable to format loans information.";
        }
    }

    private string FormatInvestmentsResponse(string jsonData)
    {
        try
        {
            var investments = System.Text.Json.JsonSerializer.Deserialize<InvestmentsResponse>(jsonData);
            if (investments?.Investments == null) return "**User Agent Error:** No investment data available.";
            
            if (investments.Investments.Length == 0)
            {
                return "**Your Investments:** You currently have no investment accounts with us.";
            }
            
            var investmentInfos = new List<string>();
            foreach (var inv in investments.Investments)
            {
                investmentInfos.Add($"**{inv.PortfolioName}** ({inv.AccountNumber})\n" +
                    $"- Total Value: ${inv.TotalValue:N2}\n" +
                    $"- Total Invested: ${inv.TotalInvested:N2}\n" +
                    $"- Total Gain/Loss: ${inv.TotalGain:N2} ({inv.GainPercentage:+0.00;-0.00}%)\n" +
                    $"- Last Updated: {inv.LastUpdated:MMM dd, yyyy HH:mm}\n" +
                    $"- Holdings: {inv.Holdings.Length} positions");
            }
            
            var investmentInfo = string.Join("\n\n", investmentInfos);
            var totalValue = investments.Investments.Sum(inv => inv.TotalValue);
            var totalGain = investments.Investments.Sum(inv => inv.TotalGain);
            var totalInvested = investments.Investments.Sum(inv => inv.TotalInvested);
            var overallGainPercentage = totalInvested > 0 ? (totalGain / totalInvested) * 100 : 0;
            
            return $@"**Your Investment Portfolio:**

{investmentInfo}

**Portfolio Summary:**
- Total Value: ${totalValue:N2}
- Total Invested: ${totalInvested:N2}
- Total Gain/Loss: ${totalGain:N2} ({overallGainPercentage:+0.00;-0.00}%)";
        }
        catch
        {
            return "**User Agent Error:** Unable to format investments information.";
        }
    }
}

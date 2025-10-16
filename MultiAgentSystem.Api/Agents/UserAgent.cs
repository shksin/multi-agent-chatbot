using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

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

    public UserAgent(IConfiguration configuration, ILogger<UserAgent> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:58551"),
            Timeout = TimeSpan.FromSeconds(30)
        };
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
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            
            var response = await _httpClient.GetStringAsync(endpoint);
            
            return formatter(response);
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
            var profile = System.Text.Json.JsonSerializer.Deserialize<ProfileResponse>(jsonData);
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
        catch
        {
            return "**User Agent Error:** Unable to format profile information.";
        }
    }

    private string FormatAccountsResponse(string jsonData)
    {
        try
        {
            var accounts = System.Text.Json.JsonSerializer.Deserialize<AccountsResponse>(jsonData);
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
        catch
        {
            return "**User Agent Error:** Unable to format accounts information.";
        }
    }

    private string FormatAccountSummaryResponse(string jsonData)
    {
        try
        {
            var accounts = System.Text.Json.JsonSerializer.Deserialize<AccountsResponse>(jsonData);
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
        catch
        {
            return "**User Agent Error:** Unable to format account summary.";
        }
    }

    private string FormatTransactionsResponse(string jsonData)
    {
        try
        {
            var transactions = System.Text.Json.JsonSerializer.Deserialize<TransactionsResponse>(jsonData);
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
        catch
        {
            return "**User Agent Error:** Unable to format transactions information.";
        }
    }

    private string FormatCardsResponse(string jsonData)
    {
        try
        {
            var cards = System.Text.Json.JsonSerializer.Deserialize<CardsResponse>(jsonData);
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
        catch
        {
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

    // Response models for API deserialization
    private class ProfileResponse
    {
        public string CustomerId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public AddressInfo Address { get; set; } = new();
        public string DateOfBirth { get; set; } = string.Empty;
        public string MemberSince { get; set; } = string.Empty;
        public string CustomerType { get; set; } = string.Empty;
        public string RelationshipManager { get; set; } = string.Empty;
        public string PreferredBranch { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
    }

    private class AddressInfo
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    private class AccountsResponse
    {
        public AccountInfo[] Accounts { get; set; } = Array.Empty<AccountInfo>();
    }

    private class AccountInfo
    {
        public string AccountId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string OpenedDate { get; set; } = string.Empty;
        public decimal InterestRate { get; set; }
    }

    private class TransactionsResponse
    {
        public TransactionInfo[] Transactions { get; set; } = Array.Empty<TransactionInfo>();
    }

    private class TransactionInfo
    {
        public string TransactionId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    private class CardsResponse
    {
        public CardInfo[] Cards { get; set; } = Array.Empty<CardInfo>();
    }

    private class CardInfo
    {
        public string CardId { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal CurrentBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public int RewardsPoints { get; set; }
    }

    private class LoansResponse
    {
        public LoanInfo[] Loans { get; set; } = Array.Empty<LoanInfo>();
    }

    private class LoanInfo
    {
        public string LoanId { get; set; } = string.Empty;
        public string LoanType { get; set; } = string.Empty;
        public string LoanNumber { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MonthlyPayment { get; set; }
        public DateTime NextPaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TermMonths { get; set; }
        public int RemainingMonths { get; set; }
    }

    private class InvestmentsResponse
    {
        public InvestmentInfo[] Investments { get; set; } = Array.Empty<InvestmentInfo>();
    }

    private class InvestmentInfo
    {
        public string InvestmentId { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PortfolioName { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal TotalGain { get; set; }
        public decimal GainPercentage { get; set; }
        public DateTime LastUpdated { get; set; }
        public HoldingInfo[] Holdings { get; set; } = Array.Empty<HoldingInfo>();
    }

    private class HoldingInfo
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Shares { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Value { get; set; }
    }
}

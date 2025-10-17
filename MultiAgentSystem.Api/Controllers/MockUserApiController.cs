using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MultiAgentSystem.Api.Models;

namespace MultiAgentSystem.Api.Controllers;

[ApiController]
[Route("api/mock-user")]
public class MockUserApiController : ControllerBase
{
    private readonly ILogger<MockUserApiController> _logger;

    public MockUserApiController(ILogger<MockUserApiController> logger)
    {
        _logger = logger;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile([FromHeader] string authorization)
    {
        try
        {
            var userName = ExtractUserNameFromToken(authorization);
            await Task.Delay(100); // Simulate network latency

            var profile = GetMockUserProfile(userName);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return BadRequest(new { error = "Unable to retrieve user profile" });
        }
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetUserAccounts([FromHeader] string authorization)
    {
        try
        {
            var userName = ExtractUserNameFromToken(authorization);
            await Task.Delay(150); // Simulate network latency

            var accounts = GetMockBankAccounts(userName);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user accounts");
            return BadRequest(new { error = "Unable to retrieve accounts" });
        }
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetRecentTransactions([FromHeader] string authorization, [FromQuery] int limit = 10)
    {
        try
        {
            var userName = ExtractUserNameFromToken(authorization);
            await Task.Delay(200); // Simulate network latency

            var transactions = GetMockTransactions(userName, limit);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transactions");
            return BadRequest(new { error = "Unable to retrieve transactions" });
        }
    }

    [HttpGet("cards")]
    public async Task<IActionResult> GetUserCards([FromHeader] string authorization)
    {
        try
        {
            var userName = ExtractUserNameFromToken(authorization);
            await Task.Delay(120); // Simulate network latency

            var cards = GetMockCards(userName);
            return Ok(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cards");
            return BadRequest(new { error = "Unable to retrieve cards" });
        }
    }

    [HttpGet("loans")]
    public async Task<IActionResult> GetUserLoans([FromHeader] string authorization)
    {
        try
        {
            var userName = ExtractUserNameFromToken(authorization);
            await Task.Delay(180); // Simulate network latency

            var loans = GetMockLoans(userName);
            return Ok(loans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loans");
            return BadRequest(new { error = "Unable to retrieve loans" });
        }
    }

    [HttpGet("investments")]
    public async Task<IActionResult> GetUserInvestments([FromHeader] string authorization)
    {
        try
        {
            var userName = ExtractUserNameFromToken(authorization);
            await Task.Delay(160); // Simulate network latency

            var investments = GetMockInvestments(userName);
            return Ok(investments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving investments");
            return BadRequest(new { error = "Unable to retrieve investments" });
        }
    }

    private string ExtractUserNameFromToken(string authorization)
    {
        try
        {
            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
                return "demo";

            var token = authorization.Substring(7);
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            return userNameClaim?.Value ?? "demo";
        }
        catch
        {
            return "demo";
        }
    }

    private ProfileResponse GetMockUserProfile(string userName)
    {
        var profiles = new Dictionary<string, ProfileResponse>
        {
            ["admin"] = new ProfileResponse
            {
                CustomerId = "CUST001",
                FirstName = "Sarah",
                LastName = "Johnson",
                Email = "sarah.johnson@email.com",
                Phone = "+1-555-0123",
                Address = new AddressInfo
                {
                    Street = "123 Main Street",
                    City = "New York",
                    State = "NY",
                    ZipCode = "10001",
                    Country = "USA"
                },
                DateOfBirth = "1985-03-15",
                MemberSince = "2018-06-20",
                CustomerType = "Premier",
                RelationshipManager = "Michael Chen",
                PreferredBranch = "Manhattan Downtown",
                LastLogin = DateTime.UtcNow.AddHours(-2)
            },
            ["user1"] = new ProfileResponse
            {
                CustomerId = "CUST002",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@email.com",
                Phone = "+1-555-0456",
                Address = new AddressInfo
                {
                    Street = "456 Oak Avenue",
                    City = "Chicago",
                    State = "IL",
                    ZipCode = "60601",
                    Country = "USA"
                },
                DateOfBirth = "1990-07-22",
                MemberSince = "2020-01-15",
                CustomerType = "Standard",
                RelationshipManager = "Lisa Rodriguez",
                PreferredBranch = "Chicago Loop",
                LastLogin = DateTime.UtcNow.AddHours(-5)
            },
            ["demo"] = new ProfileResponse
            {
                CustomerId = "CUST999",
                FirstName = "Demo",
                LastName = "User",
                Email = "demo@smartbanking.com",
                Phone = "+1-555-DEMO",
                Address = new AddressInfo
                {
                    Street = "999 Demo Street",
                    City = "Demo City",
                    State = "DC",
                    ZipCode = "00000",
                    Country = "USA"
                },
                DateOfBirth = "1995-01-01",
                MemberSince = "2024-01-01",
                CustomerType = "Trial",
                RelationshipManager = "Demo Manager",
                PreferredBranch = "Demo Branch",
                LastLogin = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        return profiles.GetValueOrDefault(userName, profiles["demo"]);
    }

    private AccountsResponse GetMockBankAccounts(string userName)
    {
        var accountsData = new Dictionary<string, AccountInfo[]>
        {
            ["admin"] = new AccountInfo[]
            {
                new AccountInfo
                {
                    AccountId = "ACC001",
                    AccountNumber = "****1234",
                    AccountType = "Checking",
                    AccountName = "Premier Checking",
                    Balance = 15750.85m,
                    Currency = "USD",
                    Status = "Active",
                    OpenedDate = "2018-06-20",
                    InterestRate = 0.25m
                },
                new AccountInfo
                {
                    AccountId = "ACC002",
                    AccountNumber = "****5678",
                    AccountType = "Savings",
                    AccountName = "High Yield Savings",
                    Balance = 45200.50m,
                    Currency = "USD",
                    Status = "Active",
                    OpenedDate = "2018-08-15",
                    InterestRate = 2.15m
                },
                new AccountInfo
                {
                    AccountId = "ACC003",
                    AccountNumber = "****9012",
                    AccountType = "Investment",
                    AccountName = "Investment Portfolio",
                    Balance = 125800.75m,
                    Currency = "USD",
                    Status = "Active",
                    OpenedDate = "2019-02-10",
                    InterestRate = 0.0m
                }
            },
            ["user1"] = new AccountInfo[]
            {
                new AccountInfo
                {
                    AccountId = "ACC004",
                    AccountNumber = "****3456",
                    AccountType = "Checking",
                    AccountName = "Standard Checking",
                    Balance = 3250.40m,
                    Currency = "USD",
                    Status = "Active",
                    OpenedDate = "2020-01-15",
                    InterestRate = 0.05m
                },
                new AccountInfo
                {
                    AccountId = "ACC005",
                    AccountNumber = "****7890",
                    AccountType = "Savings",
                    AccountName = "Regular Savings",
                    Balance = 8750.25m,
                    Currency = "USD",
                    Status = "Active",
                    OpenedDate = "2020-03-20",
                    InterestRate = 1.25m
                }
            },
            ["demo"] = new AccountInfo[]
            {
                new AccountInfo
                {
                    AccountId = "DEMO001",
                    AccountNumber = "****0000",
                    AccountType = "Checking",
                    AccountName = "Demo Checking",
                    Balance = 1000.00m,
                    Currency = "USD",
                    Status = "Active",
                    OpenedDate = "2024-01-01",
                    InterestRate = 0.01m
                }
            }
        };

        return new AccountsResponse { Accounts = accountsData.GetValueOrDefault(userName, accountsData["demo"]) };
    }

    private TransactionsResponse GetMockTransactions(string userName, int limit)
    {
        var transactionsData = new Dictionary<string, TransactionInfo[]>
        {
            ["admin"] = new TransactionInfo[]
            {
                new TransactionInfo
                {
                    TransactionId = "TXN001",
                    Date = DateTime.UtcNow.AddDays(-1),
                    Description = "Salary Deposit",
                    Amount = 5500.00m,
                    Type = "Credit",
                    Category = "Income",
                    AccountNumber = "****1234",
                    Balance = 15750.85m
                },
                new TransactionInfo
                {
                    TransactionId = "TXN002",
                    Date = DateTime.UtcNow.AddDays(-2),
                    Description = "Mortgage Payment",
                    Amount = -2200.00m,
                    Type = "Debit",
                    Category = "Housing",
                    AccountNumber = "****1234",
                    Balance = 10250.85m
                },
                new TransactionInfo
                {
                    TransactionId = "TXN003",
                    Date = DateTime.UtcNow.AddDays(-3),
                    Description = "Grocery Store",
                    Amount = -145.67m,
                    Type = "Debit",
                    Category = "Food",
                    AccountNumber = "****1234",
                    Balance = 12450.85m
                },
                new TransactionInfo
                {
                    TransactionId = "TXN004",
                    Date = DateTime.UtcNow.AddDays(-4),
                    Description = "Investment Transfer",
                    Amount = 1000.00m,
                    Type = "Credit",
                    Category = "Investment",
                    AccountNumber = "****9012",
                    Balance = 124800.75m
                },
                new TransactionInfo
                {
                    TransactionId = "TXN005",
                    Date = DateTime.UtcNow.AddDays(-5),
                    Description = "Gas Station",
                    Amount = -65.23m,
                    Type = "Debit",
                    Category = "Transportation",
                    AccountNumber = "****1234",
                    Balance = 12596.52m
                }
            },
            ["user1"] = new TransactionInfo[]
            {
                new TransactionInfo
                {
                    TransactionId = "TXN006",
                    Date = DateTime.UtcNow.AddDays(-1),
                    Description = "Paycheck Direct Deposit",
                    Amount = 2800.00m,
                    Type = "Credit",
                    Category = "Income",
                    AccountNumber = "****3456",
                    Balance = 3250.40m
                },
                new TransactionInfo
                {
                    TransactionId = "TXN007",
                    Date = DateTime.UtcNow.AddDays(-2),
                    Description = "Rent Payment",
                    Amount = -1200.00m,
                    Type = "Debit",
                    Category = "Housing",
                    AccountNumber = "****3456",
                    Balance = 450.40m
                },
                new TransactionInfo
                {
                    TransactionId = "TXN008",
                    Date = DateTime.UtcNow.AddDays(-3),
                    Description = "Coffee Shop",
                    Amount = -4.75m,
                    Type = "Debit",
                    Category = "Food",
                    AccountNumber = "****3456",
                    Balance = 1650.40m
                }
            },
            ["demo"] = new TransactionInfo[]
            {
                new TransactionInfo
                {
                    TransactionId = "DEMO_TXN001",
                    Date = DateTime.UtcNow.AddHours(-2),
                    Description = "Demo Transaction",
                    Amount = 100.00m,
                    Type = "Credit",
                    Category = "Demo",
                    AccountNumber = "****0000",
                    Balance = 1000.00m
                },
                new TransactionInfo
                {
                    TransactionId = "DEMO_TXN002",
                    Date = DateTime.UtcNow.AddHours(-4),
                    Description = "Sample Purchase",
                    Amount = -25.50m,
                    Type = "Debit",
                    Category = "Demo",
                    AccountNumber = "****0000",
                    Balance = 900.00m
                }
            }
        };

        var transactions = transactionsData.GetValueOrDefault(userName, transactionsData["demo"]);
        return new TransactionsResponse { Transactions = transactions.Take(limit).ToArray() };
    }

    private CardsResponse GetMockCards(string userName)
    {
        var cardsData = new Dictionary<string, CardInfo[]>
        {
            ["admin"] = new CardInfo[]
            {
                new CardInfo
                {
                    CardId = "CARD001",
                    CardNumber = "****1234",
                    CardType = "Credit",
                    CardName = "Premium Rewards Card",
                    CreditLimit = 15000.00m,
                    AvailableCredit = 12750.50m,
                    CurrentBalance = 2249.50m,
                    Status = "Active",
                    ExpiryDate = "12/2028",
                    RewardsPoints = 12580
                },
                new CardInfo
                {
                    CardId = "CARD002",
                    CardNumber = "****5678",
                    CardType = "Debit",
                    CardName = "Premier Debit Card",
                    CreditLimit = 0.00m,
                    AvailableCredit = 0.00m,
                    CurrentBalance = 0.00m,
                    Status = "Active",
                    ExpiryDate = "09/2027",
                    RewardsPoints = 0
                }
            },
            ["user1"] = new CardInfo[]
            {
                new CardInfo
                {
                    CardId = "CARD003",
                    CardNumber = "****9012",
                    CardType = "Credit",
                    CardName = "Standard Credit Card",
                    CreditLimit = 5000.00m,
                    AvailableCredit = 4250.75m,
                    CurrentBalance = 749.25m,
                    Status = "Active",
                    ExpiryDate = "08/2027",
                    RewardsPoints = 2340
                },
                new CardInfo
                {
                    CardId = "CARD004",
                    CardNumber = "****3456",
                    CardType = "Debit",
                    CardName = "Standard Debit Card",
                    CreditLimit = 0.00m,
                    AvailableCredit = 0.00m,
                    CurrentBalance = 0.00m,
                    Status = "Active",
                    ExpiryDate = "06/2026",
                    RewardsPoints = 0
                }
            },
            ["demo"] = new CardInfo[]
            {
                new CardInfo
                {
                    CardId = "DEMO_CARD001",
                    CardNumber = "****0000",
                    CardType = "Debit",
                    CardName = "Demo Debit Card",
                    CreditLimit = 0.00m,
                    AvailableCredit = 0.00m,
                    CurrentBalance = 0.00m,
                    Status = "Active",
                    ExpiryDate = "12/2025",
                    RewardsPoints = 0
                }
            }
        };

        return new CardsResponse { Cards = cardsData.GetValueOrDefault(userName, cardsData["demo"]) };
    }

    private LoansResponse GetMockLoans(string userName)
    {
        var loansData = new Dictionary<string, LoanInfo[]>
        {
            ["admin"] = new LoanInfo[]
            {
                new LoanInfo
                {
                    LoanId = "LOAN001",
                    LoanType = "Mortgage",
                    LoanNumber = "****HOME123",
                    OriginalAmount = 450000.00m,
                    CurrentBalance = 385200.50m,
                    InterestRate = 3.25m,
                    MonthlyPayment = 2200.00m,
                    NextPaymentDate = DateTime.UtcNow.AddDays(15),
                    Status = "Active",
                    TermMonths = 360,
                    RemainingMonths = 285
                },
                new LoanInfo
                {
                    LoanId = "LOAN002",
                    LoanType = "Auto",
                    LoanNumber = "****AUTO456",
                    OriginalAmount = 35000.00m,
                    CurrentBalance = 18750.25m,
                    InterestRate = 4.5m,
                    MonthlyPayment = 650.00m,
                    NextPaymentDate = DateTime.UtcNow.AddDays(12),
                    Status = "Active",
                    TermMonths = 60,
                    RemainingMonths = 32
                }
            },
            ["user1"] = new LoanInfo[]
            {
                new LoanInfo
                {
                    LoanId = "LOAN003",
                    LoanType = "Personal",
                    LoanNumber = "****PERS789",
                    OriginalAmount = 15000.00m,
                    CurrentBalance = 8950.75m,
                    InterestRate = 8.25m,
                    MonthlyPayment = 350.00m,
                    NextPaymentDate = DateTime.UtcNow.AddDays(20),
                    Status = "Active",
                    TermMonths = 48,
                    RemainingMonths = 28
                }
            },
            ["demo"] = new LoanInfo[]
            {
                new LoanInfo
                {
                    LoanId = "DEMO_LOAN001",
                    LoanType = "Personal",
                    LoanNumber = "****DEMO000",
                    OriginalAmount = 5000.00m,
                    CurrentBalance = 2500.00m,
                    InterestRate = 10.0m,
                    MonthlyPayment = 150.00m,
                    NextPaymentDate = DateTime.UtcNow.AddDays(10),
                    Status = "Active",
                    TermMonths = 36,
                    RemainingMonths = 18
                }
            }
        };

        return new LoansResponse { Loans = loansData.GetValueOrDefault(userName, loansData["demo"]) };
    }

    private InvestmentsResponse GetMockInvestments(string userName)
    {
        var investmentsData = new Dictionary<string, InvestmentInfo[]>
        {
            ["admin"] = new InvestmentInfo[]
            {
                new InvestmentInfo
                {
                    InvestmentId = "INV001",
                    AccountNumber = "****INV123",
                    PortfolioName = "Growth Portfolio",
                    TotalValue = 125800.75m,
                    TotalInvested = 98500.00m,
                    TotalGain = 27300.75m,
                    GainPercentage = 27.71m,
                    LastUpdated = DateTime.UtcNow.AddMinutes(-15),
                    Holdings = new[]
                    {
                        new HoldingInfo { Symbol = "AAPL", Name = "Apple Inc.", Shares = 50, CurrentPrice = 175.25m, Value = 8761.25m },
                        new HoldingInfo { Symbol = "GOOGL", Name = "Alphabet Inc.", Shares = 25, CurrentPrice = 142.50m, Value = 3562.50m },
                        new HoldingInfo { Symbol = "MSFT", Name = "Microsoft Corp.", Shares = 75, CurrentPrice = 415.75m, Value = 31181.25m },
                        new HoldingInfo { Symbol = "TSLA", Name = "Tesla Inc.", Shares = 30, CurrentPrice = 245.80m, Value = 7374.00m }
                    }
                },
                new InvestmentInfo
                {
                    InvestmentId = "INV002",
                    AccountNumber = "****401K456",
                    PortfolioName = "401(k) Retirement",
                    TotalValue = 89450.50m,
                    TotalInvested = 75200.00m,
                    TotalGain = 14250.50m,
                    GainPercentage = 18.95m,
                    LastUpdated = DateTime.UtcNow.AddMinutes(-15),
                    Holdings = new[]
                    {
                        new HoldingInfo { Symbol = "VTIAX", Name = "Vanguard Total International", Shares = 500, CurrentPrice = 28.75m, Value = 14375.00m },
                        new HoldingInfo { Symbol = "VTSAX", Name = "Vanguard Total Stock Market", Shares = 650, CurrentPrice = 115.50m, Value = 75075.50m }
                    }
                }
            },
            ["user1"] = new InvestmentInfo[]
            {
                new InvestmentInfo
                {
                    InvestmentId = "INV003",
                    AccountNumber = "****INV789",
                    PortfolioName = "Starter Portfolio",
                    TotalValue = 12750.25m,
                    TotalInvested = 10500.00m,
                    TotalGain = 2250.25m,
                    GainPercentage = 21.43m,
                    LastUpdated = DateTime.UtcNow.AddMinutes(-15),
                    Holdings = new[]
                    {
                        new HoldingInfo { Symbol = "SPY", Name = "SPDR S&P 500 ETF", Shares = 25, CurrentPrice = 445.50m, Value = 11137.50m },
                        new HoldingInfo { Symbol = "VTI", Name = "Vanguard Total Stock Market ETF", Shares = 15, CurrentPrice = 107.52m, Value = 1612.80m }
                    }
                }
            },
            ["demo"] = new InvestmentInfo[]
            {
                new InvestmentInfo
                {
                    InvestmentId = "DEMO_INV001",
                    AccountNumber = "****DEMO000",
                    PortfolioName = "Demo Portfolio",
                    TotalValue = 1050.00m,
                    TotalInvested = 1000.00m,
                    TotalGain = 50.00m,
                    GainPercentage = 5.00m,
                    LastUpdated = DateTime.UtcNow.AddMinutes(-5),
                    Holdings = new[]
                    {
                        new HoldingInfo { Symbol = "DEMO", Name = "Demo Stock", Shares = 10, CurrentPrice = 105.00m, Value = 1050.00m }
                    }
                }
            }
        };

        return new InvestmentsResponse { Investments = investmentsData.GetValueOrDefault(userName, investmentsData["demo"]) };
    }
}
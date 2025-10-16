using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

    private object GetMockUserProfile(string userName)
    {
        var profiles = new Dictionary<string, object>
        {
            ["admin"] = new
            {
                customerId = "CUST001",
                firstName = "Sarah",
                lastName = "Johnson",
                email = "sarah.johnson@email.com",
                phone = "+1-555-0123",
                address = new
                {
                    street = "123 Main Street",
                    city = "New York",
                    state = "NY",
                    zipCode = "10001",
                    country = "USA"
                },
                dateOfBirth = "1985-03-15",
                memberSince = "2018-06-20",
                customerType = "Premier",
                relationshipManager = "Michael Chen",
                preferredBranch = "Manhattan Downtown",
                lastLogin = DateTime.UtcNow.AddHours(-2)
            },
            ["user1"] = new
            {
                customerId = "CUST002",
                firstName = "John",
                lastName = "Doe",
                email = "john.doe@email.com",
                phone = "+1-555-0456",
                address = new
                {
                    street = "456 Oak Avenue",
                    city = "Chicago",
                    state = "IL",
                    zipCode = "60601",
                    country = "USA"
                },
                dateOfBirth = "1990-07-22",
                memberSince = "2020-01-15",
                customerType = "Standard",
                relationshipManager = "Lisa Rodriguez",
                preferredBranch = "Chicago Loop",
                lastLogin = DateTime.UtcNow.AddHours(-5)
            },
            ["demo"] = new
            {
                customerId = "CUST999",
                firstName = "Demo",
                lastName = "User",
                email = "demo@smartbanking.com",
                phone = "+1-555-DEMO",
                address = new
                {
                    street = "999 Demo Street",
                    city = "Demo City",
                    state = "DC",
                    zipCode = "00000",
                    country = "USA"
                },
                dateOfBirth = "1995-01-01",
                memberSince = "2024-01-01",
                customerType = "Trial",
                relationshipManager = "Demo Manager",
                preferredBranch = "Demo Branch",
                lastLogin = DateTime.UtcNow.AddMinutes(-30)
            }
        };

        return profiles.GetValueOrDefault(userName, profiles["demo"]);
    }

    private object GetMockBankAccounts(string userName)
    {
        var accountsData = new Dictionary<string, object[]>
        {
            ["admin"] = new object[]
            {
                new
                {
                    accountId = "ACC001",
                    accountNumber = "****1234",
                    accountType = "Checking",
                    accountName = "Premier Checking",
                    balance = 15750.85m,
                    currency = "USD",
                    status = "Active",
                    openedDate = "2018-06-20",
                    interestRate = 0.25m
                },
                new
                {
                    accountId = "ACC002",
                    accountNumber = "****5678",
                    accountType = "Savings",
                    accountName = "High Yield Savings",
                    balance = 45200.50m,
                    currency = "USD",
                    status = "Active",
                    openedDate = "2018-08-15",
                    interestRate = 2.15m
                },
                new
                {
                    accountId = "ACC003",
                    accountNumber = "****9012",
                    accountType = "Investment",
                    accountName = "Investment Portfolio",
                    balance = 125800.75m,
                    currency = "USD",
                    status = "Active",
                    openedDate = "2019-02-10",
                    interestRate = 0.0m
                }
            },
            ["user1"] = new object[]
            {
                new
                {
                    accountId = "ACC004",
                    accountNumber = "****3456",
                    accountType = "Checking",
                    accountName = "Standard Checking",
                    balance = 3250.40m,
                    currency = "USD",
                    status = "Active",
                    openedDate = "2020-01-15",
                    interestRate = 0.05m
                },
                new
                {
                    accountId = "ACC005",
                    accountNumber = "****7890",
                    accountType = "Savings",
                    accountName = "Regular Savings",
                    balance = 8750.25m,
                    currency = "USD",
                    status = "Active",
                    openedDate = "2020-03-20",
                    interestRate = 1.25m
                }
            },
            ["demo"] = new object[]
            {
                new
                {
                    accountId = "DEMO001",
                    accountNumber = "****0000",
                    accountType = "Checking",
                    accountName = "Demo Checking",
                    balance = 1000.00m,
                    currency = "USD",
                    status = "Active",
                    openedDate = "2024-01-01",
                    interestRate = 0.01m
                }
            }
        };

        return new { accounts = accountsData.GetValueOrDefault(userName, accountsData["demo"]) };
    }

    private object GetMockTransactions(string userName, int limit)
    {
        var transactionsData = new Dictionary<string, object[]>
        {
            ["admin"] = new object[]
            {
                new
                {
                    transactionId = "TXN001",
                    date = DateTime.UtcNow.AddDays(-1),
                    description = "Salary Deposit",
                    amount = 5500.00m,
                    type = "Credit",
                    category = "Income",
                    accountNumber = "****1234",
                    balance = 15750.85m
                },
                new
                {
                    transactionId = "TXN002",
                    date = DateTime.UtcNow.AddDays(-2),
                    description = "Mortgage Payment",
                    amount = -2200.00m,
                    type = "Debit",
                    category = "Housing",
                    accountNumber = "****1234",
                    balance = 10250.85m
                },
                new
                {
                    transactionId = "TXN003",
                    date = DateTime.UtcNow.AddDays(-3),
                    description = "Grocery Store",
                    amount = -145.67m,
                    type = "Debit",
                    category = "Food",
                    accountNumber = "****1234",
                    balance = 12450.85m
                },
                new
                {
                    transactionId = "TXN004",
                    date = DateTime.UtcNow.AddDays(-4),
                    description = "Investment Transfer",
                    amount = 1000.00m,
                    type = "Credit",
                    category = "Investment",
                    accountNumber = "****9012",
                    balance = 124800.75m
                },
                new
                {
                    transactionId = "TXN005",
                    date = DateTime.UtcNow.AddDays(-5),
                    description = "Gas Station",
                    amount = -65.23m,
                    type = "Debit",
                    category = "Transportation",
                    accountNumber = "****1234",
                    balance = 12596.52m
                }
            },
            ["user1"] = new object[]
            {
                new
                {
                    transactionId = "TXN006",
                    date = DateTime.UtcNow.AddDays(-1),
                    description = "Paycheck Direct Deposit",
                    amount = 2800.00m,
                    type = "Credit",
                    category = "Income",
                    accountNumber = "****3456",
                    balance = 3250.40m
                },
                new
                {
                    transactionId = "TXN007",
                    date = DateTime.UtcNow.AddDays(-2),
                    description = "Rent Payment",
                    amount = -1200.00m,
                    type = "Debit",
                    category = "Housing",
                    accountNumber = "****3456",
                    balance = 450.40m
                },
                new
                {
                    transactionId = "TXN008",
                    date = DateTime.UtcNow.AddDays(-3),
                    description = "Coffee Shop",
                    amount = -4.75m,
                    type = "Debit",
                    category = "Food",
                    accountNumber = "****3456",
                    balance = 1650.40m
                }
            },
            ["demo"] = new object[]
            {
                new
                {
                    transactionId = "DEMO_TXN001",
                    date = DateTime.UtcNow.AddHours(-2),
                    description = "Demo Transaction",
                    amount = 100.00m,
                    type = "Credit",
                    category = "Demo",
                    accountNumber = "****0000",
                    balance = 1000.00m
                },
                new
                {
                    transactionId = "DEMO_TXN002",
                    date = DateTime.UtcNow.AddHours(-4),
                    description = "Sample Purchase",
                    amount = -25.50m,
                    type = "Debit",
                    category = "Demo",
                    accountNumber = "****0000",
                    balance = 900.00m
                }
            }
        };

        var transactions = transactionsData.GetValueOrDefault(userName, transactionsData["demo"]);
        return new { transactions = transactions.Take(limit).ToArray() };
    }

    private object GetMockCards(string userName)
    {
        var cardsData = new Dictionary<string, object[]>
        {
            ["admin"] = new object[]
            {
                new
                {
                    cardId = "CARD001",
                    cardNumber = "****1234",
                    cardType = "Credit",
                    cardName = "Premium Rewards Card",
                    creditLimit = 15000.00m,
                    availableCredit = 12750.50m,
                    currentBalance = 2249.50m,
                    status = "Active",
                    expiryDate = "12/2028",
                    rewardsPoints = 12580
                },
                new
                {
                    cardId = "CARD002",
                    cardNumber = "****5678",
                    cardType = "Debit",
                    cardName = "Premier Debit Card",
                    creditLimit = 0.00m,
                    availableCredit = 0.00m,
                    currentBalance = 0.00m,
                    status = "Active",
                    expiryDate = "09/2027",
                    rewardsPoints = 0
                }
            },
            ["user1"] = new object[]
            {
                new
                {
                    cardId = "CARD003",
                    cardNumber = "****9012",
                    cardType = "Credit",
                    cardName = "Standard Credit Card",
                    creditLimit = 5000.00m,
                    availableCredit = 4250.75m,
                    currentBalance = 749.25m,
                    status = "Active",
                    expiryDate = "08/2027",
                    rewardsPoints = 2340
                },
                new
                {
                    cardId = "CARD004",
                    cardNumber = "****3456",
                    cardType = "Debit",
                    cardName = "Standard Debit Card",
                    creditLimit = 0.00m,
                    availableCredit = 0.00m,
                    currentBalance = 0.00m,
                    status = "Active",
                    expiryDate = "06/2026",
                    rewardsPoints = 0
                }
            },
            ["demo"] = new object[]
            {
                new
                {
                    cardId = "DEMO_CARD001",
                    cardNumber = "****0000",
                    cardType = "Debit",
                    cardName = "Demo Debit Card",
                    creditLimit = 0.00m,
                    availableCredit = 0.00m,
                    currentBalance = 0.00m,
                    status = "Active",
                    expiryDate = "12/2025",
                    rewardsPoints = 0
                }
            }
        };

        return new { cards = cardsData.GetValueOrDefault(userName, cardsData["demo"]) };
    }

    private object GetMockLoans(string userName)
    {
        var loansData = new Dictionary<string, object[]>
        {
            ["admin"] = new object[]
            {
                new
                {
                    loanId = "LOAN001",
                    loanType = "Mortgage",
                    loanNumber = "****HOME123",
                    originalAmount = 450000.00m,
                    currentBalance = 385200.50m,
                    interestRate = 3.25m,
                    monthlyPayment = 2200.00m,
                    nextPaymentDate = DateTime.UtcNow.AddDays(15),
                    status = "Active",
                    termMonths = 360,
                    remainingMonths = 285
                },
                new
                {
                    loanId = "LOAN002",
                    loanType = "Auto",
                    loanNumber = "****AUTO456",
                    originalAmount = 35000.00m,
                    currentBalance = 18750.25m,
                    interestRate = 4.5m,
                    monthlyPayment = 650.00m,
                    nextPaymentDate = DateTime.UtcNow.AddDays(12),
                    status = "Active",
                    termMonths = 60,
                    remainingMonths = 32
                }
            },
            ["user1"] = new object[]
            {
                new
                {
                    loanId = "LOAN003",
                    loanType = "Personal",
                    loanNumber = "****PERS789",
                    originalAmount = 15000.00m,
                    currentBalance = 8950.75m,
                    interestRate = 8.25m,
                    monthlyPayment = 350.00m,
                    nextPaymentDate = DateTime.UtcNow.AddDays(20),
                    status = "Active",
                    termMonths = 48,
                    remainingMonths = 28
                }
            },
            ["demo"] = new object[]
            {
                new
                {
                    loanId = "DEMO_LOAN001",
                    loanType = "Personal",
                    loanNumber = "****DEMO000",
                    originalAmount = 5000.00m,
                    currentBalance = 2500.00m,
                    interestRate = 10.0m,
                    monthlyPayment = 150.00m,
                    nextPaymentDate = DateTime.UtcNow.AddDays(10),
                    status = "Active",
                    termMonths = 36,
                    remainingMonths = 18
                }
            }
        };

        return new { loans = loansData.GetValueOrDefault(userName, loansData["demo"]) };
    }

    private object GetMockInvestments(string userName)
    {
        var investmentsData = new Dictionary<string, object[]>
        {
            ["admin"] = new object[]
            {
                new
                {
                    investmentId = "INV001",
                    accountNumber = "****INV123",
                    portfolioName = "Growth Portfolio",
                    totalValue = 125800.75m,
                    totalInvested = 98500.00m,
                    totalGain = 27300.75m,
                    gainPercentage = 27.71m,
                    lastUpdated = DateTime.UtcNow.AddMinutes(-15),
                    holdings = new[]
                    {
                        new { symbol = "AAPL", name = "Apple Inc.", shares = 50, currentPrice = 175.25m, value = 8761.25m },
                        new { symbol = "GOOGL", name = "Alphabet Inc.", shares = 25, currentPrice = 142.50m, value = 3562.50m },
                        new { symbol = "MSFT", name = "Microsoft Corp.", shares = 75, currentPrice = 415.75m, value = 31181.25m },
                        new { symbol = "TSLA", name = "Tesla Inc.", shares = 30, currentPrice = 245.80m, value = 7374.00m }
                    }
                },
                new
                {
                    investmentId = "INV002",
                    accountNumber = "****401K456",
                    portfolioName = "401(k) Retirement",
                    totalValue = 89450.50m,
                    totalInvested = 75200.00m,
                    totalGain = 14250.50m,
                    gainPercentage = 18.95m,
                    lastUpdated = DateTime.UtcNow.AddMinutes(-15),
                    holdings = new[]
                    {
                        new { symbol = "VTIAX", name = "Vanguard Total International", shares = 500, currentPrice = 28.75m, value = 14375.00m },
                        new { symbol = "VTSAX", name = "Vanguard Total Stock Market", shares = 650, currentPrice = 115.50m, value = 75075.50m }
                    }
                }
            },
            ["user1"] = new object[]
            {
                new
                {
                    investmentId = "INV003",
                    accountNumber = "****INV789",
                    portfolioName = "Starter Portfolio",
                    totalValue = 12750.25m,
                    totalInvested = 10500.00m,
                    totalGain = 2250.25m,
                    gainPercentage = 21.43m,
                    lastUpdated = DateTime.UtcNow.AddMinutes(-15),
                    holdings = new[]
                    {
                        new { symbol = "SPY", name = "SPDR S&P 500 ETF", shares = 25, currentPrice = 445.50m, value = 11137.50m },
                        new { symbol = "VTI", name = "Vanguard Total Stock Market ETF", shares = 15, currentPrice = 107.52m, value = 1612.80m }
                    }
                }
            },
            ["demo"] = new object[]
            {
                new
                {
                    investmentId = "DEMO_INV001",
                    accountNumber = "****DEMO000",
                    portfolioName = "Demo Portfolio",
                    totalValue = 1050.00m,
                    totalInvested = 1000.00m,
                    totalGain = 50.00m,
                    gainPercentage = 5.00m,
                    lastUpdated = DateTime.UtcNow.AddMinutes(-5),
                    holdings = new[]
                    {
                        new { symbol = "DEMO", name = "Demo Stock", shares = 10, currentPrice = 105.00m, value = 1050.00m }
                    }
                }
            }
        };

        return new { investments = investmentsData.GetValueOrDefault(userName, investmentsData["demo"]) };
    }
}
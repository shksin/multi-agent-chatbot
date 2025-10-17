namespace MultiAgentSystem.Api.Models;

// Profile Models
public class ProfileResponse
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

public class AddressInfo
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

// Account Models
public class AccountsResponse
{
    public AccountInfo[] Accounts { get; set; } = Array.Empty<AccountInfo>();
}

public class AccountInfo
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

// Transaction Models
public class TransactionsResponse
{
    public TransactionInfo[] Transactions { get; set; } = Array.Empty<TransactionInfo>();
}

public class TransactionInfo
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

// Card Models
public class CardsResponse
{
    public CardInfo[] Cards { get; set; } = Array.Empty<CardInfo>();
}

public class CardInfo
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

// Loan Models
public class LoansResponse
{
    public LoanInfo[] Loans { get; set; } = Array.Empty<LoanInfo>();
}

public class LoanInfo
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

// Investment Models
public class InvestmentsResponse
{
    public InvestmentInfo[] Investments { get; set; } = Array.Empty<InvestmentInfo>();
}

public class InvestmentInfo
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

public class HoldingInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Shares { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal Value { get; set; }
}

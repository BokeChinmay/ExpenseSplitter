namespace ExpenseSplitter.Web.Models;

public record AuthResponse(string Token, int UserId, string Name, string Email);

public record GroupDto(int Id, string Name, string Currency, List<MemberDto> Members, int ExpenseCount, decimal TotalAmount);

public record MemberDto(int UserId, string Name, string Email);

public record ExpenseDto(int Id, string Description, decimal Amount, string Category, string PaidByName, int PaidByUserId, DateTime Date, List<SplitDto> Splits);

public record SplitDto(int UserId, string UserName, decimal Amount, bool IsSettled);

public record SettlementSuggestion(int FromUserId, string FromUserName, int ToUserId, string ToUserName, decimal Amount);

public record GroupInsightsDto(Dictionary<string, decimal> SpendingByCategory, Dictionary<string, decimal> SpendingByMember, List<MonthlySpendDto> MonthlyTrend, string AiSummary);

public record MonthlySpendDto(string Month, decimal Amount);

public record ParsedReceiptDto(string Description, decimal Amount, string Category, DateTime Date);

public record CreateExpenseRequest(string Description, decimal Amount, string Category, int SplitType, DateTime Date, List<CustomSplitDto>? CustomSplits);

public record CustomSplitDto(int UserId, decimal Amount);
namespace ExpenseSplitter.Api.Models;

//Auth 
public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, int UserId, string Name, string Email);

//Groups
public record CreateGroupRequest(string Name, string Currency);
public record AddMemberRequest(string Email);

public record GroupDto(int Id, string Name, string Currency, List<MemberDto> Members, int ExpenseCount, decimal TotalAmount);
public record MemberDto(int UserId, string Name, string Email);

//Expenses
public record CreateExpenseRequest(string Description, decimal Amount, string Category, SplitType SplitType, DateTime Date, List<CustomSplitDto>? CustomSplits);
public record CustomSplitDto(int UserId, decimal Amount);

public record ExpenseDto(int Id, string Description, decimal Amount, string Category, string PaidByName, int PaidByUserId, DateTime Date, List<SplitDto> Splits);
public record SplitDto(int UserId, string UserName, decimal Amount, bool IsSettled);

//Settlements
public record SettlementSuggestion(int FromUserId, string FromUserName, int ToUserId, string ToUserName, decimal Amount);

//Stats
public record GroupStatsDto(decimal TotalSpent, Dictionary<string, decimal> SpendingByCategory, Dictionary<string, decimal> SpendingByMember, List<MonthlySpendDto> MonthlyTrend);
public record MonthlySpendDto(string Month, decimal Amount);
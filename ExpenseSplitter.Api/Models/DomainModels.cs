namespace ExpenseSplitter.Api.Models;

public class User{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<GroupMember> GroupMemberships { get; set; } = new();
    public List<Expense> PaidExpenses { get; set; } = new();
}

public class Group {
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<GroupMember> Members { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
}

public class GroupMember {
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class Expense {
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int PaidByUserId { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Category { get; set; } = "General";
    public SplitType SplitType { get; set; } = SplitType.Equal;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Group Group { get; set; } = null!;
    public User PaidBy { get; set; } = null!;
    public List<ExpenseSplit> Splits { get; set; } = new(); 
}

public class ExpenseSplit
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public bool IsSettled { get; set; } = false;

    public Expense Expense { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class Settlement
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime SettledAt { get; set; } = DateTime.UtcNow;

    public Group Group { get; set; } = null!;
    public User FromUser { get; set; } = null!;
    public User ToUser { get; set; } = null!;
}

public enum SplitType { Equal, Percentage, Custom }
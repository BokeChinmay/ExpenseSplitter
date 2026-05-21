using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseSplitter.Api.Services;

public class ExpenseService {
    private readonly AppDbContext _db;
    private readonly DebtSimplificationService _debtService;

    public ExpenseService(AppDbContext db, DebtSimplificationService debtService) {
        _db = db;
        _debtService = debtService;
    }

    //Groups
    public async Task<GroupDto> CreateGroupAsync (int userId, CreateGroupRequest request) {
        var group = new Group {
            Name = request.Name,
            Currency = request.Currency
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        //Creator automatically joins the group
        _db.GroupMembers.Add(new GroupMember {
            GroupId = group.Id,
            UserId = userId
        });

        await _db.SaveChangesAsync();
        return await GetGroupDtoAsync(group.Id);
    }

    public async Task<List<GroupDto>> GetUserGroupsAsync(int userId) {

        var groupIds = await _db.GroupMembers.Where(gm => gm.UserId == userId).Select(gm => gm.GroupId).ToListAsync();

        var groups = new List<GroupDto>();
        foreach (var id in groupIds) {
            groups.Add(await GetGroupDtoAsync(id));
        }

        return groups;
    }

    public async Task<bool> AddMemberAsync(int groupId, int requestingUserId, AddMemberRequest request) {
        //Verify if the reqesting user is in the group
        var isMember = await _db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == requestingUserId);
        if(!isMember) return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return false;

        var alreadyMember = await _db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == user.Id);
        if(alreadyMember) return false;

        _db.GroupMembers.Add(new GroupMember {
            GroupId = groupId,
            UserId = user.Id
        });

        await _db.SaveChangesAsync();
        return true;
    }

    //Expenses
    public async Task<ExpenseDto> CreateExpenseAsync(
        int groupId, int paidByUserId, CreateExpenseRequest request)
    {
        var members = await _db.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User)
            .ToListAsync();

        var expense = new Expense
        {
            GroupId = groupId,
            PaidByUserId = paidByUserId,
            Description = request.Description,
            Amount = request.Amount,
            Category = request.Category,
            SplitType = request.SplitType,
            Date = request.Date
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        // Calculate splits based on type
        var splits = CalculateSplits(
            expense.Id, request.Amount, request.SplitType,
            members.Select(m => m.UserId).ToList(),
            request.CustomSplits);

        _db.ExpenseSplits.AddRange(splits);
        await _db.SaveChangesAsync();

        return await GetExpenseDtoAsync(expense.Id);
    }

    public async Task<List<ExpenseDto>> GetGroupExpensesAsync(int groupId)
    {
        var expenseIds = await _db.Expenses
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.Date)
            .Select(e => e.Id)
            .ToListAsync();

        var dtos = new List<ExpenseDto>();
        foreach (var id in expenseIds)
            dtos.Add(await GetExpenseDtoAsync(id));

        return dtos;
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId, int userId)
    {
        var expense = await _db.Expenses
            .Include(e => e.Splits)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense == null || expense.PaidByUserId != userId)
            return false;

        _db.ExpenseSplits.RemoveRange(expense.Splits);
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        return true;
    }

    //Settlements
    public async Task<List<SettlementSuggestion>> GetSettlementsAsync(int groupId)
    {
        var members = await _db.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Include(gm => gm.User)
            .Select(gm => new { gm.UserId, gm.User.Name })
            .ToListAsync();

        var expenses = await _db.Expenses
            .Where(e => e.GroupId == groupId)
            .Include(e => e.Splits)
            .ToListAsync();

        var expenseData = expenses.Select(e => (
            PaidByUserId: e.PaidByUserId,
            Amount: e.Amount,
            Splits: e.Splits.Select(s => (s.UserId, s.Amount)).ToList()
        )).ToList();

        var memberData = members
            .Select(m => (m.UserId, m.Name))
            .ToList();

        var balances = _debtService.CalculateBalances(memberData, expenseData);
        return _debtService.Simplify(balances);
    }

    public async Task<Settlement> RecordSettlementAsync(
        int groupId, int fromUserId, int toUserId, decimal amount)
    {
        var settlement = new Settlement
        {
            GroupId = groupId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount
        };

        _db.Settlements.Add(settlement);

        // Mark relevant splits as settled
        var splits = await _db.ExpenseSplits
            .Include(s => s.Expense)
            .Where(s => s.UserId == fromUserId &&
                       s.Expense.GroupId == groupId &&
                       !s.IsSettled)
            .OrderBy(s => s.Expense.Date)
            .ToListAsync();

        var remaining = amount;
        foreach (var split in splits)
        {
            if (remaining <= 0) break;
            if (split.Amount <= remaining)
            {
                split.IsSettled = true;
                remaining -= split.Amount;
            }
        }

        await _db.SaveChangesAsync();
        return settlement;
    }

    //Stats
    public async Task<GroupStatsDto> GetGroupStatsAsync(int groupId)
    {
        var expenses = await _db.Expenses
            .Where(e => e.GroupId == groupId)
            .Include(e => e.PaidBy)
            .Include(e => e.Splits)
            .ToListAsync();

        var totalSpent = expenses.Sum(e => e.Amount);

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var byMember = expenses
            .GroupBy(e => e.PaidBy.Name)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var monthlyTrend = expenses
            .GroupBy(e => e.Date.ToString("MMM yyyy"))
            .Select(g => new MonthlySpendDto(g.Key, g.Sum(e => e.Amount)))
            .OrderBy(m => m.Month)
            .ToList();

        return new GroupStatsDto(totalSpent, byCategory, byMember, monthlyTrend);
    }

    //Helpers
    private List<ExpenseSplit> CalculateSplits(
        int expenseId, decimal totalAmount, SplitType splitType,
        List<int> memberIds, List<CustomSplitDto>? customSplits)
    {
        return splitType switch
        {
            SplitType.Equal => memberIds.Select(uid => new ExpenseSplit
            {
                ExpenseId = expenseId,
                UserId = uid,
                Amount = Math.Round(totalAmount / memberIds.Count, 2)
            }).ToList(),

            SplitType.Percentage => customSplits?.Select(s => new ExpenseSplit
            {
                ExpenseId = expenseId,
                UserId = s.UserId,
                Amount = Math.Round(totalAmount * (s.Amount / 100), 2)
            }).ToList() ?? new List<ExpenseSplit>(),

            SplitType.Custom => customSplits?.Select(s => new ExpenseSplit
            {
                ExpenseId = expenseId,
                UserId = s.UserId,
                Amount = s.Amount
            }).ToList() ?? new List<ExpenseSplit>(),

            _ => new List<ExpenseSplit>()
        };
    }

    private async Task<GroupDto> GetGroupDtoAsync(int groupId)
    {
        var group = await _db.Groups
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .Include(g => g.Expenses)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) throw new KeyNotFoundException($"Group {groupId} not found");

        return new GroupDto(
            group.Id, group.Name, group.Currency,
            group.Members.Select(m => new MemberDto(
                m.UserId, m.User.Name, m.User.Email)).ToList(),
            group.Expenses.Count,
            group.Expenses.Sum(e => e.Amount)
        );
    }

    private async Task<ExpenseDto> GetExpenseDtoAsync(int expenseId)
    {
        var expense = await _db.Expenses
            .Include(e => e.PaidBy)
            .Include(e => e.Splits)
            .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense == null) throw new KeyNotFoundException($"Expense {expenseId} not found");

        return new ExpenseDto(
            expense.Id,
            expense.Description,
            expense.Amount,
            expense.Category,
            expense.PaidBy.Name,
            expense.PaidByUserId,
            expense.Date,
            expense.Splits.Select(s => new SplitDto(
                s.UserId, s.User.Name, s.Amount, s.IsSettled)).ToList()
        );
    }
}
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseSplitter.Api.Services;

public class ExpenseService {
    private readonly AppDbContext _db;

    public ExpenseService(AppDbContext db) {
        _db = db;
    }

    public async Task<List<GroupDto>> GetUserGroupsAsync(int userId) {
        return await _db.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .Select(g => new GroupDto(
                g.Id, g.Name, g.Currency, 
                g.Members.Select(m => new MemberDto(m.UserId, m.User.Name, m.User.Email)).ToList(),
                g.Expenses.Count,
                g.Expenses.Sum(e => e.Amount)
            ))
            .ToListAsync();
    }
}
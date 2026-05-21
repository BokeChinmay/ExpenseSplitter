using System.Security.Claims;
using ExpenseSplitter.Api.Models;
using ExpenseSplitter.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase {
    private readonly ExpenseService _expenseService;

    public GroupsController(ExpenseService expenseService) {
        _expenseService = expenseService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyGroups() {
        var groups = await _expenseService.GetUserGroupsAsync(GetUserId());
        return Ok(groups);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request) {
        var group = await _expenseService.CreateGroupAsync(GetUserId(), request);
        return CreatedAtAction(nameof(GetMyGroups), new { id = group.Id }, group);
    }

    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberRequest request) {
        var success = await _expenseService.AddMemberAsync(groupId, GetUserId(), request);
        if(!success) return BadRequest("User not found or already a member.");
        return Ok();
    }

    [HttpGet("{groupId}/expenses")]
    public async Task<IActionResult> GetExpenses(int groupId) {
        var expenses = await _expenseService.GetGroupExpensesAsync(groupId);
        return Ok(expenses);
    }

    [HttpPost("{groupId}/expenses")]
    public async Task<IActionResult> CreateExpense(int groupId, [FromBody] CreateExpenseRequest request) {
        var expense = await _expenseService.CreateExpenseAsync(groupId, GetUserId(), request);
        return CreatedAtAction(nameof(GetExpenses), new { groupId }, expense);
    }

    [HttpDelete("{groupId}/expenses/{expenseId}")]
    public async Task<IActionResult> DeleteExpense(int groupId, int expenseId) {
        var success = await _expenseService.DeleteExpenseAsync(expenseId, GetUserId());
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("{groupId}/settlements")]
    public async Task<IActionResult> GetSettlements(int groupId) {
        var settlements = await _expenseService.GetSettlementsAsync(groupId);
        return Ok(settlements);
    }

    [HttpPost("{groupId}/settlements")]
    public async Task<IActionResult> RecordSettlement(int groupId, [FromBody] RecordSettlementRequest request) {
        var settlement = await _expenseService.RecordSettlementAsync(groupId, GetUserId(), request.ToUserId, request.Amount);
        return Ok(settlement);
    }

    [HttpGet("{groupId}/stats")]
    public async Task<IActionResult> GetStats(int groupId) {
        var stats = await _expenseService.GetGroupStatsAsync(groupId);
        return Ok(stats);
    }
}

public record RecordSettlementRequest(int ToUserId, decimal Amount);
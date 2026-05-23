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
    private readonly ReceiptParsingService _receiptService;
    private readonly InsightsService _insightsService;

    public GroupsController(ExpenseService expenseService, ReceiptParsingService receiptService, InsightsService insightsService) {
        _expenseService = expenseService;
        _receiptService = receiptService;
        _insightsService = insightsService;
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

    [HttpPost("parse-receipt")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> ParseReceipt() {
        if (!Request.HasFormContentType)
            return BadRequest("Expected multipart form data.");

        var form = await Request.ReadFormAsync();

        var file = form.Files.GetFile("file");

        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        //Detect content type from extenstion if browser didn't send it
        var contentType = file.ContentType;
        if(string.IsNullOrEmpty(contentType) || contentType == "application/octet-stream") {
            var ext = Path.GetExtension(file.FileName).ToLower();
            contentType = ext switch {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".heic" => "image/heic",
                _ => "application/octet-stream"
            };
        }

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic" };
        Console.WriteLine($"Allowed check: '{contentType.ToLower()}' in allowed = {allowed.Contains(contentType.ToLower())}");

        if (!allowed.Contains(contentType.ToLower()))
            return BadRequest($"Unsupported file type: {contentType}");

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("File must be under 5MB.");

        try {
            using var stream = file.OpenReadStream();
            var result = await _receiptService.ParseReceiptAsync(stream, contentType);
            return Ok(result);
        }
        catch (Exception ex) {
            return StatusCode(500, $"Receipt parsing failed: {ex.Message}");
        }
    }

    [HttpGet("{groupId}/insights")]
    public async Task<IActionResult> GetInsights(int groupId) {
        var insights = await _insightsService.GetInsightsAsync(groupId);
        return Ok(insights);
    }
}

public record RecordSettlementRequest(int ToUserId, decimal Amount);
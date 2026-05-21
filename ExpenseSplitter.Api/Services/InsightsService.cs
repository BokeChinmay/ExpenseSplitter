using System.Text;
using System.Text.Json;
using ExpenseSplitter.Api.Data;
using ExpenseSplitter.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseSplitter.Api.Services;

public class InsightsService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public InsightsService(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<GroupInsightsDto> GetInsightsAsync(int groupId)
    {
        var expenses = await _db.Expenses
            .Where(e => e.GroupId == groupId)
            .Include(e => e.PaidBy)
            .ToListAsync();

        if (!expenses.Any())
            return new GroupInsightsDto(
                new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>(),
                new List<MonthlySpendDto>(),
                "No expenses yet, add some to see insights.");

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

        var totalSpent = expenses.Sum(e => e.Amount);
        var topCategory = byCategory.MaxBy(kvp => kvp.Value).Key;
        var topSpender = byMember.MaxBy(kvp => kvp.Value).Key;

        // Generate AI summary
        var summary = await GenerateSummaryAsync(
            totalSpent, byCategory, byMember, topCategory, topSpender);

        return new GroupInsightsDto(byCategory, byMember, monthlyTrend, summary);
    }

    private async Task<string> GenerateSummaryAsync(
        decimal totalSpent,
        Dictionary<string, decimal> byCategory,
        Dictionary<string, decimal> byMember,
        string topCategory,
        string topSpender)
    {
        var categoryBreakdown = string.Join(", ",
            byCategory.Select(kvp => $"{kvp.Key}: ${kvp.Value:F2}"));

        var memberBreakdown = string.Join(", ",
            byMember.Select(kvp => $"{kvp.Key}: ${kvp.Value:F2}"));

        var prompt = $"""
            Summarize this group's spending in 2-3 sentences. Be specific with numbers.
            Total spent: ${totalSpent:F2}
            By category: {categoryBreakdown}
            By member: {memberBreakdown}
            Top category: {topCategory}
            Top spender: {topSpender}
            Keep it friendly and concise. No bullet points.
            """;

        var http = _httpFactory.CreateClient();
        var body = JsonSerializer.Serialize(new
        {
            model = "llama-3.3-70b-versatile",
            max_tokens = 150,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Unable to generate summary.";
        }
        catch
        {
            return $"Total spent: ${totalSpent:F2}. Top category: {topCategory}. Top spender: {topSpender}.";
        }
    }
}

public record GroupInsightsDto(
    Dictionary<string, decimal> SpendingByCategory,
    Dictionary<string, decimal> SpendingByMember,
    List<MonthlySpendDto> MonthlyTrend,
    string AiSummary
);
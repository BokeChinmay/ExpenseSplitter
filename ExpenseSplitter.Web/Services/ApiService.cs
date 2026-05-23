using System.Net.Http.Json;
using ExpenseSplitter.Web.Models;

namespace ExpenseSplitter.Web.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http) { _http = http; }

    // Auth
    public async Task<AuthResponse?> RegisterAsync(string name, string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register",
            new { name, email, password });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login",
            new { email, password });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResponse>();
    }

    // Groups
    public async Task<List<GroupDto>> GetGroupsAsync()
    {
        try { return await _http.GetFromJsonAsync<List<GroupDto>>("api/groups") ?? new(); }
        catch { return new(); }
    }

    public async Task<GroupDto?> CreateGroupAsync(string name, string currency)
    {
        var response = await _http.PostAsJsonAsync("api/groups", new { name, currency });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<GroupDto>();
    }

    public async Task<bool> AddMemberAsync(int groupId, string email)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/groups/{groupId}/members", new { email });
        return response.IsSuccessStatusCode;
    }

    // Expenses
    public async Task<List<ExpenseDto>> GetExpensesAsync(int groupId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ExpenseDto>>(
                $"api/groups/{groupId}/expenses") ?? new();
        }
        catch { return new(); }
    }

    public async Task<ExpenseDto?> CreateExpenseAsync(int groupId, CreateExpenseRequest request)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/groups/{groupId}/expenses", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ExpenseDto>();
    }

    public async Task<bool> DeleteExpenseAsync(int groupId, int expenseId)
    {
        var response = await _http.DeleteAsync(
            $"api/groups/{groupId}/expenses/{expenseId}");
        return response.IsSuccessStatusCode;
    }

    // Settlements
    public async Task<List<SettlementSuggestion>> GetSettlementsAsync(int groupId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<SettlementSuggestion>>(
                $"api/groups/{groupId}/settlements") ?? new();
        }
        catch { return new(); }
    }

    // Insights
    public async Task<GroupInsightsDto?> GetInsightsAsync(int groupId)
    {
        try
        {
            return await _http.GetFromJsonAsync<GroupInsightsDto>(
                $"api/groups/{groupId}/insights");
        }
        catch { return null; }
    }

    // Receipt parsing
    public async Task<ParsedReceiptDto?> ParseReceiptAsync(Stream imageStream, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        ms.Position = 0;
        content.Add(new StreamContent(ms), "file", fileName);

        var response = await _http.PostAsync("api/groups/parse-receipt", content);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ParsedReceiptDto>();
    }
}
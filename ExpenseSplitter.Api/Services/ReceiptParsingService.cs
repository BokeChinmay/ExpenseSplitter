using System.Text;
using System.Text.Json;

namespace ExpenseSplitter.Api.Services;

public class ReceiptParsingService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public ReceiptParsingService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<ParsedReceiptDto> ParseReceiptAsync(Stream imageStream, string contentType)
    {
        // Convert image to base64
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());

        var http = _httpFactory.CreateClient();

        var body = JsonSerializer.Serialize(new
        {
            model = "meta-llama/llama-4-scout-17b-16e-instruct",
            max_tokens = 500,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{contentType};base64,{base64}"
                            }
                        },
                        new
                        {
                            type = "text",
                            text = """
                                Analyze this receipt image and extract the following information.
                                Respond ONLY with a valid JSON object, no explanation, no markdown:
                                {
                                  "description": "brief merchant name and what was purchased",
                                  "amount": total amount as a number,
                                  "category": one of ["Food", "Transport", "Accommodation", "Entertainment", "Shopping", "Utilities", "General"],
                                  "date": "YYYY-MM-DD format or null if not visible"
                                }
                                If you cannot determine a value, use null.
                                """
                        }
                    }
                }
            }
        });

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {_config["Groq:ApiKey"]}");
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        // Strip markdown fences if model wraps response
        content = content
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        try
        {
            using var resultDoc = JsonDocument.Parse(content);
            var root = resultDoc.RootElement;

            return new ParsedReceiptDto(
                Description: root.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? "" : "",
                Amount: root.TryGetProperty("amount", out var amt) &&
                    amt.ValueKind == JsonValueKind.Number
                    ? amt.GetDecimal() : 0,
                Category: root.TryGetProperty("category", out var cat)
                    ? cat.GetString() ?? "General" : "General",
                Date: root.TryGetProperty("date", out var date) &&
                    date.ValueKind == JsonValueKind.String &&
                    DateTime.TryParse(date.GetString(), out var parsedDate)
                    ? parsedDate : DateTime.UtcNow
            );
        }
        catch
        {
            return new ParsedReceiptDto("", 0, "General", DateTime.UtcNow);
        }
    }
}

public record ParsedReceiptDto(
    string Description,
    decimal Amount,
    string Category,
    DateTime Date
);
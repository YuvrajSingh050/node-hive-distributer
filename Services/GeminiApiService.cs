using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NodeHiveCenter.Services;

public class GeminiApiService
{
    private const string ApiKey = "AIzaSyBi-XdLNt4kMytpHnm37JAFssSXMHzfbNw";
    private const string Endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={ApiKey}";

    private readonly HttpClient _http = new();

    public async Task<string> GenerateAsync(string prompt)
    {
        try
        {
            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json    = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonNode.Parse(responseJson);
            return doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>()
                   ?? "[No response]";
        }
        catch (Exception ex)
        {
            return $"[Error contacting Gemini: {ex.Message}]";
        }
    }
}

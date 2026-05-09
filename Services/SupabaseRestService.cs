using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NodeHiveCenter.Services;

public class SupabaseRestService
{
    private const string BaseUrl = "https://gyqzckyjxlcqqdfugcgg.supabase.co/rest/v1/";
    private const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imd5cXpja3lqeGxjcXFkZnVnY2dnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzczNjIyOTAsImV4cCI6MjA5MjkzODI5MH0.bn53GTMchXySu9pYlV87RvL93mmsaL3s3RfYxrDbVjE";

    private readonly HttpClient _http;

    public SupabaseRestService()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _http.DefaultRequestHeaders.Add("apikey", AnonKey);
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {AnonKey}");
        _http.DefaultRequestHeaders.Add("Prefer", "return=minimal");
    }

    public async Task UpdateSessionAsync(Dictionary<string, object?> fields)
    {
        try
        {
            var json = JsonSerializer.Serialize(fields);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PatchAsync("session?id=eq.1", content);
            // Silent on non-success — UI drives state locally anyway
        }
        catch
        {
            // Network errors are non-fatal; local state still advances
        }
    }
}

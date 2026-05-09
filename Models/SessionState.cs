namespace NodeHiveCenter.Models;

public class SessionState
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public string? Prompt { get; set; }
    public string? Result { get; set; }
    public string? NodeAStatus { get; set; }
    public string? NodeBStatus { get; set; }
}

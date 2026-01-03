namespace ProjectBrain.Domain.Dtos;

public class EmailMessage
{
    public required string To { get; set; }
    public string? Subject { get; set; }
    public string? Template { get; set; }
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public string? From { get; set; }
    public Dictionary<string, object>? Variables { get; set; }
    public List<string>? Cc { get; set; }
    public List<string>? Bcc { get; set; }
}


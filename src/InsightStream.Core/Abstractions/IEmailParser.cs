namespace InsightStream.Core.Abstractions;

public interface IEmailParser
{
    ParsedEmail Parse(byte[] rawEmail);
}

public record ParsedEmail(string Subject, string DestinationEmail, DateTimeOffset Date, string HtmlBody);

using InsightStream.Core.Abstractions;
using MimeKit;

namespace InsightStream.Infrastructure.Parsing;

public class MimeKitEmailParser : IEmailParser
{
    public ParsedEmail Parse(byte[] rawEmail)
    {
        using var stream = new MemoryStream(rawEmail);
        var message = MimeMessage.Load(stream);

        var destinationEmail = message.To.Mailboxes.FirstOrDefault()?.Address
            ?? message.To.ToString();

        var htmlBody = message.HtmlBody ?? string.Empty;

        return new ParsedEmail(
            Subject: message.Subject ?? string.Empty,
            DestinationEmail: destinationEmail,
            Date: message.Date,
            HtmlBody: htmlBody
        );
    }
}

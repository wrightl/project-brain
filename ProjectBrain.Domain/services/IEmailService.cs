namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public interface IEmailService
{
    /// <summary>
    /// Sends an email using the provided parameters.
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML content of the email</param>
    /// <param name="textBody">Plain text content of the email (optional)</param>
    /// <param name="from">Sender email address (optional, uses default if not provided)</param>
    /// <param name="cc">List of CC recipients (optional)</param>
    /// <param name="bcc">List of BCC recipients (optional)</param>
    Task SendEmailAsync(
        string to,
        string? subject = null,
        string? template = null,
        string? htmlBody = null,
        string? textBody = null,
        string? from = null,
        Dictionary<string, object>? variables = null,
        List<string>? cc = null,
        List<string>? bcc = null);

    /// <summary>
    /// Sends an email using an EmailMessage object.
    /// </summary>
    /// <param name="message">Email message containing all email details</param>
    Task SendEmailAsync(EmailMessage message);
}


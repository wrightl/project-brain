namespace ProjectBrain.Domain;

using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;

public class MailgunEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailgunEmailService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IFeatureFlagService _featureFlagService;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public MailgunEmailService(
        IConfiguration configuration,
        ILogger<MailgunEmailService> logger,
        [FromKeyedServices("Mailgun")] HttpClient httpClient,
        IFeatureFlagService featureFlagService)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _featureFlagService = featureFlagService;

        // Read Mailgun configuration
        _fromEmail = _configuration["Mailgun:FromEmail"]
            ?? throw new InvalidOperationException("Mailgun:FromEmail is not configured");
        _fromName = _configuration["Mailgun:FromName"] ?? "ProjectBrain";
    }

    public async Task SendEmailAsync(
        string to,
        string? subject = null,
        string? template = null,
        string? htmlBody = null,
        string? textBody = null,
        string? from = null,
        Dictionary<string, object>? variables = null,
        List<string>? cc = null,
        List<string>? bcc = null)
    {
        var message = new EmailMessage
        {
            To = to,
            Subject = subject,
            HtmlBody = htmlBody,
            Template = template,
            TextBody = textBody,
            From = from,
            Variables = variables,
            Cc = cc,
            Bcc = bcc
        };

        await SendEmailAsync(message);
    }

    public async Task SendEmailAsync(EmailMessage message)
    {
        // Check if emails are enabled via feature flag
        var emailsEnabled = await _featureFlagService.IsEmailingEnabled();
        if (!emailsEnabled)
        {
            _logger.LogWarning(
                "Email sending is disabled via feature flag. Skipping email to {To} with subject {Subject}",
                message.To,
                message.Subject);
            return;
        }

        try
        {
            // Use default from email if not provided
            var fromEmail = message.From ?? _fromEmail;
            var fromAddress = string.IsNullOrWhiteSpace(_fromName)
                ? fromEmail
                : $"{_fromName} <{fromEmail}>";

            // Build form data
            var formData = new MultipartFormDataContent
            {
                { new StringContent(fromAddress), "from" },
                { new StringContent(message.To), "to" },
            };

            if (!string.IsNullOrWhiteSpace(message.Template))
            {
                formData.Add(new StringContent(message.Template), "template");
                if (message.Variables != null && message.Variables.Any())
                {
                    var variablesJson = JsonSerializer.Serialize(message.Variables);
                    formData.Add(new StringContent(variablesJson), "h:X-Mailgun-Variables");
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(message.Subject))
                    formData.Add(new StringContent(message.Subject), "subject");
                // Add html body if provided
                if (!string.IsNullOrWhiteSpace(message.HtmlBody))
                    formData.Add(new StringContent(message.HtmlBody), "html");
                // Add text body if provided
                if (!string.IsNullOrWhiteSpace(message.TextBody))
                    formData.Add(new StringContent(message.TextBody), "text");
            }
            // Add CC recipients if provided
            if (message.Cc != null && message.Cc.Any())
            {
                foreach (var ccEmail in message.Cc)
                {
                    formData.Add(new StringContent(ccEmail), "cc");
                }
            }

            // Add BCC recipients if provided
            if (message.Bcc != null && message.Bcc.Any())
            {
                foreach (var bccEmail in message.Bcc)
                {
                    formData.Add(new StringContent(bccEmail), "bcc");
                }
            }

            // Send request to Mailgun API
            var response = await _httpClient.PostAsync("messages", formData);


            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to send email via Mailgun. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode,
                    errorContent);
                throw new InvalidOperationException(
                    $"Failed to send email via Mailgun. Status: {response.StatusCode}, Response: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(
                "Email sent successfully via Mailgun to {To}. Subject: {Subject}",
                message.To,
                message.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To} with subject {Subject}", message.To, message.Subject);
            throw;
        }
    }
}


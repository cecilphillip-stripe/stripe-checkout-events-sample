using SendGrid;
using SendGrid.Helpers.Mail;

namespace StripeEventsCheckout.ApiServer.Services;

public class SendGridMessageSender : IMessageSender
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridMessageSender> _logger;

    public SendGridMessageSender(ISendGridClient sendGridClient, IConfiguration configuration, ILogger<SendGridMessageSender> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
    }
    public async Task SendMessageAsync(string message, string receiver)
    {
        var from = new EmailAddress(_configuration["SendGrid:FromAddress"], "Stripe Event Demo");
        var to = new EmailAddress(receiver);

        var msg = new SendGridMessage
        {
            From = from,
            Subject = "Your ticket order is complete"
        };

        msg.AddContent(MimeType.Text, message);        
        msg.AddTo(to);

        var emailResponse = await _sendGridClient.SendEmailAsync(msg);
        _logger.LogInformation("Email sent status => {EmailResponseStatusCode}", emailResponse.StatusCode);
    }
}
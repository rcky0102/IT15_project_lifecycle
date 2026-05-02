using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;

namespace project_lifecycle.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            msg.To.Add(MailboxAddress.Parse(email));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = htmlMessage };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_settings.User))
            {
                await client.AuthenticateAsync(_settings.User, _settings.Password).ConfigureAwait(false);
            }
            await client.SendAsync(msg).ConfigureAwait(false);
            await client.DisconnectAsync(true).ConfigureAwait(false);
        }
    }
}

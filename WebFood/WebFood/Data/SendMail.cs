using MailKit.Net.Smtp;
using MimeKit;

namespace WebFood.Data
{
    public class SendMail
    {
        private readonly IConfiguration _configuration;

        public SendMail(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var portString = _configuration["EmailSettings:Port"];
            var fromEmail = _configuration["EmailSettings:SenderEmail"];
            var fromPassword = _configuration["EmailSettings:SenderPassword"];

            // Thử chuyển đổi port thành số và xử lý nếu không hợp lệ
            if (!int.TryParse(portString, out var port))
            {
                throw new ArgumentException("Port không hợp lệ", nameof(portString));
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using (var client = new SmtpClient())
            {
                client.Connect(smtpServer, port, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(fromEmail, fromPassword);
                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}

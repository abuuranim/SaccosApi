using System.Net.Mail;
using System.Net;

namespace SaccosApi.Services
{
    public class EmailNotificationService
    {
        public  void SendEmail(string smtpServer, int smtpPort, string senderEmail, string senderName, string username, string password, string recipientEmail, string subject, string body)
        {
            try
            {
                var client = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(recipientEmail);

                client.Send(mailMessage);
                Console.WriteLine("Email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught in SendEmail(): {ex}");
            }
        }
    }
}

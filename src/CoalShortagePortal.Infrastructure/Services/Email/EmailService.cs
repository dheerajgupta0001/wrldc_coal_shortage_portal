using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CoalShortagePortal.Application.Interfaces;

namespace CoalShortagePortal.Infrastructure.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderName = _configuration["EmailSettings:SenderName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(username, password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = message,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email sent successfully to {toEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send email to {toEmail}: {ex.Message}");
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var subject = "Your OTP for Coal Shortage Portal";
            var message = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #333; text-align: center;'>Coal Shortage Portal</h2>
                        <p style='font-size: 16px;'>Hello,</p>
                        <p style='font-size: 16px;'>Your One-Time Password (OTP) for login is:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <span style='font-size: 32px; font-weight: bold; color: #007bff; letter-spacing: 5px; padding: 15px 30px; border: 2px dashed #007bff; display: inline-block; border-radius: 5px;'>
                                {otp}
                            </span>
                        </div>
                        <p style='font-size: 14px; color: #666;'>
                            This OTP is valid for <strong>5 minutes</strong>. Please do not share this code with anyone.
                        </p>
                        <p style='font-size: 14px; color: #666;'>
                            If you did not request this OTP, please ignore this email.
                        </p>
                        <hr style='margin: 20px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='font-size: 12px; color: #999; text-align: center;'>
                            This is an automated message, please do not reply.
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, message);
        }
    }
}
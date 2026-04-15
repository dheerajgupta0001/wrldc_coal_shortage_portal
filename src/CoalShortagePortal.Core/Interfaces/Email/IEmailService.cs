using System.Threading.Tasks;

namespace CoalShortagePortal.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
        Task SendOtpEmailAsync(string toEmail, string otp);
    }
}
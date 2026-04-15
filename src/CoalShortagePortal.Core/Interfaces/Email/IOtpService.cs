using System.Threading.Tasks;

namespace CoalShortagePortal.Application.Interfaces
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string userId, string purpose = "Login");
        Task<bool> ValidateOtpAsync(string userId, string otp, string purpose = "Login");
        Task InvalidateOtpAsync(string userId, string purpose = "Login");
    }
}
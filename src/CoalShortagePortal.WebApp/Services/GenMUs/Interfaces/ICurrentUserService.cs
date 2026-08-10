using CoalShortagePortal.WebApp.Models.GenMUs;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CoalShortagePortal.WebApp.Services.GenMUs.Interfaces
{
    public interface ICurrentUserService
    {
        Task<CurrentUserInfo> GetCurrentUserInfoAsync(ClaimsPrincipal user);
    }
}

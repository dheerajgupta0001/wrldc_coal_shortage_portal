using System.Security.Claims;
using System.Threading.Tasks;
using CoalShortagePortal.Application.Features.GenMUs.Models;

namespace CoalShortagePortal.Application.Features.GenMUs.Interfaces
{
    public interface ICurrentUserService
    {
        Task<CurrentUserInfo> GetCurrentUserInfoAsync(ClaimsPrincipal user);
    }
}

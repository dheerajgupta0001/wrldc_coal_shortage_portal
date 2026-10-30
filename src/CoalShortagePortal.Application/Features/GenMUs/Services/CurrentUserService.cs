using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CoalShortagePortal.Application.Features.GenMUs.Interfaces;
using CoalShortagePortal.Application.Features.GenMUs.Models;
using CoalShortagePortal.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoalShortagePortal.Application.Features.GenMUs.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public CurrentUserService(
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<CurrentUserInfo> GetCurrentUserInfoAsync(
            ClaimsPrincipal principal)
        {
            var user = await _userManager.GetUserAsync(principal);

            if (user == null)
            {
                return new CurrentUserInfo
                {
                    IsAdmin = false,
                    IsSldc = false,
                    State = null
                };
            }

            if (user.UserName.Equals(
                "admin",
                StringComparison.OrdinalIgnoreCase))
            {
                return new CurrentUserInfo
                {
                    IsAdmin = true,
                    IsSldc = false,
                    State = null
                };
            }

            var userDetails = await _context.UserDetails
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (userDetails != null && userDetails.IsSldc == true)
            {
                return new CurrentUserInfo
                {
                    IsAdmin = false,
                    IsSldc = true,
                    State = userDetails.State
                };
            }

            return new CurrentUserInfo
            {
                IsAdmin = false,
                IsSldc = false,
                State = null
            };
        }
    }
}
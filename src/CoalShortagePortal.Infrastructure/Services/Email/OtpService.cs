using CoalShortagePortal.Application.Interfaces;
using CoalShortagePortal.Core.Entities;
using CoalShortagePortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace CoalShortagePortal.Infrastructure.Services.Email
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OtpService> _logger;
        private const int OTP_EXPIRY_MINUTES = 5;

        public OtpService(ApplicationDbContext context, ILogger<OtpService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateOtpAsync(string userId, string purpose = "Login")
        {
            // Generate 6-digit OTP
            //var random = new Random();
            //var otp = random.Next(100000, 999999).ToString();

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Invalidate any existing OTPs for this user and purpose
            await InvalidateOtpAsync(userId, purpose);

            // Create new OTP record
            var userOtp = new UserOtp
            {
                UserId = userId,
                OtpCode = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                IsUsed = false,
                Purpose = purpose,
                Created = DateTime.UtcNow,
                CreatedById = userId
            };

            _context.UserOtps.Add(userOtp);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"OTP generated for user {userId} with purpose {purpose}");

            return otp;
        }

        public async Task<bool> ValidateOtpAsync(string userId, string otp, string purpose = "Login")
        {
            var userOtp = await _context.UserOtps
                .Where(u => u.UserId == userId
                         && u.OtpCode == otp
                         && u.Purpose == purpose
                         && !u.IsUsed
                         && u.ExpiryTime > DateTime.UtcNow)
                .OrderByDescending(u => u.Created)
                .FirstOrDefaultAsync();

            if (userOtp != null)
            {
                // Mark OTP as used
                userOtp.IsUsed = true;
                userOtp.LastModified = DateTime.UtcNow;
                userOtp.LastModifiedById = userId;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"OTP validated successfully for user {userId}");
                return true;
            }

            _logger.LogWarning($"OTP validation failed for user {userId}");
            return false;
        }

        public async Task InvalidateOtpAsync(string userId, string purpose = "Login")
        {
            var existingOtps = await _context.UserOtps
                .Where(u => u.UserId == userId && u.Purpose == purpose && !u.IsUsed)
                .ToListAsync();

            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
                otp.LastModified = DateTime.UtcNow;
            }

            if (existingOtps.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Invalidated {existingOtps.Count} existing OTPs for user {userId}");
            }
        }
    }
}
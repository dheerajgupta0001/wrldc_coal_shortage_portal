using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CoalShortagePortal.Infrastructure.Services
{
    public class OtpAttemptInfo
    {
        public List<DateTime> Attempts { get; set; } = new List<DateTime>();
        public DateTime? BlockedUntil { get; set; }
    }

    public interface IOtpRateLimitService
    {
        bool IsBlocked(string userId);
        bool RegisterFailedAttempt(string userId);
        void ResetAttempts(string userId);
        int GetRemainingAttempts(string userId);
        DateTime? GetBlockedUntil(string userId);
    }

    public class OtpRateLimitService : IOtpRateLimitService
    {
        // In-memory store: userId -> attempt info
        private static readonly ConcurrentDictionary<string, OtpAttemptInfo> _attempts
            = new ConcurrentDictionary<string, OtpAttemptInfo>();

        private const int MAX_ATTEMPTS = 5;                  // Max OTP attempts allowed
        private const int WINDOW_MINUTES = 10;               // Time window to count attempts
        private const int BLOCK_MINUTES = 5;                // Block duration after max attempts

        public bool IsBlocked(string userId)
        {
            if (!_attempts.TryGetValue(userId, out var info))
                return false;

            // Check if block has expired
            if (info.BlockedUntil.HasValue)
            {
                if (DateTime.UtcNow < info.BlockedUntil.Value)
                    return true; // Still blocked

                // Block expired — reset
                // ✅ Block expired — clean up automatically
                _attempts.TryRemove(userId, out _);
                //ResetAttempts(userId);
                return false;
            }

            return false;
        }

        public bool RegisterFailedAttempt(string userId)
        {
            var info = _attempts.GetOrAdd(userId, _ => new OtpAttemptInfo());

            lock (info)
            {
                // Remove attempts outside the time window
                var windowStart = DateTime.UtcNow.AddMinutes(-WINDOW_MINUTES);
                info.Attempts.RemoveAll(a => a < windowStart);

                // Add current attempt
                info.Attempts.Add(DateTime.UtcNow);

                // Check if max attempts reached
                if (info.Attempts.Count >= MAX_ATTEMPTS)
                {
                    info.BlockedUntil = DateTime.UtcNow.AddMinutes(BLOCK_MINUTES);
                    return true; // Now blocked
                }

                return false; // Not yet blocked
            }
        }

        public void ResetAttempts(string userId)
        {
            _attempts.TryRemove(userId, out _);
        }

        public int GetRemainingAttempts(string userId)
        {
            if (!_attempts.TryGetValue(userId, out var info))
                return MAX_ATTEMPTS;

            var windowStart = DateTime.UtcNow.AddMinutes(-WINDOW_MINUTES);
            var recentAttempts = info.Attempts.Count(a => a >= windowStart);

            return Math.Max(0, MAX_ATTEMPTS - recentAttempts);
        }

        public DateTime? GetBlockedUntil(string userId)
        {
            if (!_attempts.TryGetValue(userId, out var info))
                return null;

            return info.BlockedUntil;
        }
    }
}
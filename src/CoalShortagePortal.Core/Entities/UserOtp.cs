using CoalShortagePortal.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace CoalShortagePortal.Core.Entities
{
    public class UserOtp : AuditableEntity, IAggregateRoot
    {
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; }

        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; }

        [Required]
        public DateTime ExpiryTime { get; set; }

        public bool IsUsed { get; set; } = false;

        [MaxLength(50)]
        public string Purpose { get; set; } // "Login", "PasswordReset", etc.
    }
}
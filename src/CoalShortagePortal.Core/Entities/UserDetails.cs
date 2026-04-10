using System;
using CoalShortagePortal.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CoalShortagePortal.Core.Entities
{
    public class UserDetails : AuditableEntity, IAggregateRoot
    {
        [Required]
        [MaxLength(450)] // AspNetUsers.Id is nvarchar(450)
        public string UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string UserType { get; set; } // ISGS, RE, IPP, State_Gen

        [MaxLength(100)]
        public string State { get; set; } // Only for State_Gen

        // Navigation property to AspNetUsers
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
    }
}
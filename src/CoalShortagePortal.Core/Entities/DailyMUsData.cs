using CoalShortagePortal.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoalShortagePortal.Core.Entities
{
    public class DailyMUsData : AuditableEntity, IAggregateRoot
    {
        [Column(TypeName = "date")]
        [Required]
        public DateTime DataDate { get; set; }

        [Required]
        public float DailyMUs { get; set; }

        public float ExBus { get; set; }

        [Required]
        public string StationName { get; set; }

    }
}

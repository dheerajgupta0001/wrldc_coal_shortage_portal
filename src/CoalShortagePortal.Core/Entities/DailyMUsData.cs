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

        public float ScheduleMUs { get; set; }

        public float ExBus { get; set; }

        // Peak MW fields
        [Required]
        public int PeakMW { get; set; }

        [Required]
        public TimeSpan PeakMWTime { get; set; }

        // Off Peak MW fields
        [Required]
        public int OffPeakMW { get; set; }

        [Required]
        public TimeSpan OffPeakMWTime { get; set; }

        // Day Peak MW fields
        [Required]
        public int DayPeakMW { get; set; }

        [Required]
        public TimeSpan DayPeakMWTime { get; set; }

        // Min Generation fields
        [Required]
        public int MinGeneration { get; set; }

        [Required]
        public TimeSpan MinGenerationTime { get; set; }

        [Required]
        public string StationName { get; set; }

    }
}

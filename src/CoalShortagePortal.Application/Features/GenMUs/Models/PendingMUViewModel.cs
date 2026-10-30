using System;

namespace CoalShortagePortal.Application.Features.GenMUs.Models
{
    public class PendingMUViewModel
    {
        public string StationName { get; set; }
        public string Email { get; set; }
        public bool HasSubmitted { get; set; }
        public string UserType { get; set; }
        public float? ExistingMUs { get; set; }
        public float? ExistingScheduleMUs { get; set; }
        public float? ExistingExBus { get; set; }
        public int? ExistingPeakMW { get; set; }
        public TimeSpan? ExistingPeakMWTime { get; set; }
        public int? ExistingOffPeakMW { get; set; }
        public TimeSpan? ExistingOffPeakMWTime { get; set; }
        public int? ExistingDayPeakMW { get; set; }
        public TimeSpan? ExistingDayPeakMWTime { get; set; }
        public int? ExistingMinGeneration { get; set; }
        public TimeSpan? ExistingMinGenerationTime { get; set; }
        public bool IsScheduleUser => UserType == "ISGS" || UserType == "IPP";
    }
}

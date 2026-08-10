using System;
using System.ComponentModel.DataAnnotations;

namespace CoalShortagePortal.WebApp.Models.GenMUs
{
    public class MUEntryModel
    {
        public string StationName { get; set; }

        public string UserType { get; set; }

        public bool IsEditMode { get; set; }

        [Range(0.01, 99999.99,
            ErrorMessage = "Gross must be between 0.01 and 99,999.99")]
        public float? DailyMUs { get; set; }

        [Range(0.01, 99999.99,
            ErrorMessage = "Schedule must be between 0.01 and 99,999.99")]
        public float? ScheduleMUs { get; set; }

        [Range(0.01, 99999.99,
            ErrorMessage = "ExBus must be between 0.01 and 99,999.99")]
        public float? ExBus { get; set; }

        [Range(1, 99999,
            ErrorMessage = "Peak MW must be between 1 and 99,999")]
        public int? PeakMW { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? PeakMWTime { get; set; }

        [Range(1, 99999,
            ErrorMessage = "Off Peak MW must be between 1 and 99,999")]
        public int? OffPeakMW { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? OffPeakMWTime { get; set; }

        [Range(1, 99999,
            ErrorMessage = "Day Peak MW must be between 1 and 99,999")]
        public int? DayPeakMW { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? DayPeakMWTime { get; set; }

        [Range(1, 99999,
            ErrorMessage = "Min Generation must be between 1 and 99,999")]
        public int? MinGeneration { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? MinGenerationTime { get; set; }

        public bool IsScheduleUser => UserType == "ISGS" || UserType == "IPP";
    }
}

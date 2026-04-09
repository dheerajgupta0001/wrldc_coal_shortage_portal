using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CoalShortagePortal.Core.Entities;

namespace CoalShortagePortal.WebApp.Models
{
    public class StageListVM
    {
        public List<GenStnStg> Stages { get; set; }
    }

    public class StageCreateVM
    {
        [Required(ErrorMessage = "Station Name is required")]
        [Display(Name = "Station Name")]
        public string StationName { get; set; }

        [Required(ErrorMessage = "Stage number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Stage must be a positive number")]
        [Display(Name = "Stage Number")]
        public int Stage { get; set; }

        public List<string> AvailableStations { get; set; }
    }

    public class StageEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Station Name is required")]
        [Display(Name = "Station Name")]
        public string StationName { get; set; }

        [Required(ErrorMessage = "Stage number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Stage must be a positive number")]
        [Display(Name = "Stage Number")]
        public int Stage { get; set; }

        public List<string> AvailableStations { get; set; }
    }

    public class StageDeleteVM
    {
        public int Id { get; set; }
        public string StationName { get; set; }
        public int Stage { get; set; }
    }
}
using System;
using CoalShortagePortal.Core.Interfaces;
using System.ComponentModel.DataAnnotations;


namespace CoalShortagePortal.Core.Entities
{
    public class WrStates
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string StateName { get; set; }
    }
}
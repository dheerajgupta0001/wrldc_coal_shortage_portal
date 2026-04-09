using CoalShortagePortal.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoalShortagePortal.Core.Entities
{
    public class GenStnStg : AuditableEntity, IAggregateRoot
    {

        [Required]
        public int Stage { get; set; }

        [Required]
        public string StationName { get; set; }

    }
}

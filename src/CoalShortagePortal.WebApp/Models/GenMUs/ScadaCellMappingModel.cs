namespace CoalShortagePortal.WebApp.Models.GenMUs
{
    public class ScadaCellMappingModel
    {
        public int Id { get; set; }
        public string GeneratingStationName { get; set; }
        public string StationTypeName { get; set; }
        public int? Fuel { get; set; }
        public string FuelType { get; set; }
        public string CellReference { get; set; }
    }
}

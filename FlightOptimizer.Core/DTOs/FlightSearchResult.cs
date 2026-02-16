namespace FlightOptimizer.Core.DTOs
{
    public class FlightSearchResult
    {
        public List<string> Route { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public double TotalDurationMinutes { get; set; }
        public int TotalStops { get; set; }
        public List<List<double>> PathCoordinates { get; set; } = new(); // [Lat, Lon]
    }
}

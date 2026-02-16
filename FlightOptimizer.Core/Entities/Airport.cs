namespace FlightOptimizer.Core.Entities
{
    public class Airport
    {
        public int Id { get; set; }
        public required string IataCode { get; set; }
        public required string Name { get; set; }
        public required string City { get; set; }
        public required string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

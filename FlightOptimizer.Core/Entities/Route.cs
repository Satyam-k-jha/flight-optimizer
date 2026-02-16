using System.ComponentModel.DataAnnotations.Schema;

namespace FlightOptimizer.Core.Entities
{
    public class Route
    {
        public int Id { get; set; }

        public int SourceAirportId { get; set; }
        public required Airport SourceAirport { get; set; }

        public int DestAirportId { get; set; }
        public required Airport DestAirport { get; set; }

        public required string AirlineCode { get; set; }
        public int Stops { get; set; }

        // Generated/Calculated properties
        public decimal Price { get; set; }
        public double DurationMinutes { get; set; }
    }
}

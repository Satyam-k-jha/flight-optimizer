using NetTopologySuite.Geometries;

namespace FlightOptimizer.Core.Entities
{
    public class RestrictedZone
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required Polygon Region { get; set; }
    }
}

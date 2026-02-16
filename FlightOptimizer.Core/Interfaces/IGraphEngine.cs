using FlightOptimizer.Core.Entities;
using FlightOptimizer.Core.DTOs;

namespace FlightOptimizer.Core.Interfaces
{
    public interface IRouteFindingStrategy
    {
        FlightSearchResult FindRoute(string sourceIata, string destIata, Dictionary<string, List<Route>> adjacencyList);
    }

    public interface IGraphEngine
    {
        PathResult FindPath(string source, string dest, RouteCriteria criteria);
        IEnumerable<RestrictedZone> GetRestrictedZones();
        IEnumerable<Airport> GetAirports();
    }

    public enum RouteCriteria
    {
        Cheapest,
        Fastest,
        Layover
    }
}

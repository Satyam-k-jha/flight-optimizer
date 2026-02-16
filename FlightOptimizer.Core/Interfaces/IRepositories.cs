using FlightOptimizer.Core.Entities;

namespace FlightOptimizer.Core.Interfaces
{
    public interface IAirportRepository
    {
        Task<IEnumerable<Airport>> GetAllAsync();
        Task<Airport?> GetByIataAsync(string iata);
    }

    public interface IRouteRepository
    {
        Task<IEnumerable<Route>> GetAllAsync();
    }
                     
    // Marker interface for restricted zones if needed, or just a generic repo
    public interface IRestrictedZoneRepository
    {
        Task<IEnumerable<RestrictedZone>> GetAllAsync();
    }
}

using FlightOptimizer.Core.Entities;
using FlightOptimizer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using FlightOptimizer.Infrastructure.Data;

namespace FlightOptimizer.Infrastructure.Repositories
{
    public class AirportRepository : IAirportRepository
    {
        private readonly FlightOptimizer.Infrastructure.Data.ApplicationDbContext _context;

        public AirportRepository(FlightOptimizer.Infrastructure.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Airport>> GetAllAsync()
        {
            return await _context.Airports.ToListAsync();
        }

        public async Task<Airport?> GetByIataAsync(string iata)
        {
            return await _context.Airports.FirstOrDefaultAsync(a => a.IataCode == iata);
        }
    }

    public class RouteRepository : IRouteRepository
    {
        private readonly FlightOptimizer.Infrastructure.Data.ApplicationDbContext _context;

        public RouteRepository(FlightOptimizer.Infrastructure.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Route>> GetAllAsync()
        {
            return await _context.Routes
                // We might need to include airports if the graph engine needs their cached data directly from route object, 
                // but usually GraphEngine uses IDs. Let's include for safety or detailed projection.
                .Include(r => r.SourceAirport) 
                .Include(r => r.DestAirport)
                .ToListAsync();
        }
    }

      public class RestrictedZoneRepository : IRestrictedZoneRepository
    {
        private readonly FlightOptimizer.Infrastructure.Data.ApplicationDbContext _context;

        public RestrictedZoneRepository(FlightOptimizer.Infrastructure.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RestrictedZone>> GetAllAsync()
        {
            return await _context.RestrictedZones.ToListAsync();
        }
    }
}

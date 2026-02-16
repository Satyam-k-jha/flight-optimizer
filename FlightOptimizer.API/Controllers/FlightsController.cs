using FlightOptimizer.Core.DTOs;
using FlightOptimizer.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightOptimizer.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsController : ControllerBase
    {
        private readonly IGraphEngine _graphEngine;
        private readonly IAirportRepository _airportRepository;

        public FlightsController(IGraphEngine graphEngine, IAirportRepository airportRepository)
        {
            _graphEngine = graphEngine;
            _airportRepository = airportRepository;
        }

        [HttpGet("search")]
        public ActionResult<PathResult> Search(
            [FromQuery] string source, 
            [FromQuery] string destination, 
            [FromQuery] RouteCriteria criteria)
        {
            var result = _graphEngine.FindPath(source.ToUpper(), destination.ToUpper(), criteria);
            
            if (!result.Success)
            {
                if (result.Reason == FailureReason.RestrictedZoneBlock)
                {
                    return Conflict(result); // 409 Conflict for Restricted Zones
                }
                if (result.Reason == FailureReason.NoRoute)
                {
                    return NotFound(result); // 404 No Route
                }
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("search-airports")]
        public async Task<ActionResult<IEnumerable<object>>> SearchAirports([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return BadRequest("Query must be at least 2 characters.");
            }

            // Simple search implementation
            // Ideally this should be in Repository, but for now strict implementation here
            var allAirports = await _airportRepository.GetAllAsync();
            var matches = allAirports
                .Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                            a.City.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                            a.IataCode.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(a => new { a.IataCode, a.Name, a.City, a.Country })
                .Take(10); // Limit results

            return Ok(matches);
        }

        [HttpGet("airports")]
        public async Task<ActionResult<IEnumerable<object>>> GetAirports()
        {
            var airports = await _airportRepository.GetAllAsync();
            return Ok(airports.Select(a => new { a.IataCode, a.Name, a.City, a.Country }));
        }

        [HttpGet("restricted-zones")]
        public ActionResult<IEnumerable<object>> GetRestrictedZones()
        {
            var zones = _graphEngine.GetRestrictedZones();
            return Ok(zones.Select(z => new 
            { 
               z.Name, 
               Coordinates = z.Region.Coordinates.Select(c => new [] { c.Y, c.X }) // Lat, Lon for Leaflet
            }));
        }
    }
}

using FlightOptimizer.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FlightOptimizer.API.Controllers
{
    [ApiController]
    [Route("api/airports")]
    public class AirportsController : ControllerBase
    {
        private readonly IGraphEngine _graphEngine;

        public AirportsController(IGraphEngine graphEngine)
        {
            _graphEngine = graphEngine;
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<object>> Search([FromQuery] string query)
        {
            Console.WriteLine($"[AirportsController] Search called with query: '{query}'");
            
            if (query == "DEBUG") return Ok(new [] { "DEBUG_MODE_ACTIVE" });

            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be empty.");
            }

            // Use GraphEngine as source of truth (In-Memory Cache)
            var allAirports = _graphEngine.GetAirports();
            Console.WriteLine($"[AirportsController] Retrieved {allAirports.Count()} airports from GraphEngine.");
            
            var matches = allAirports
                .Where(a => (a.Name != null && a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) || 
                            (a.City != null && a.City.Contains(query, StringComparison.OrdinalIgnoreCase)) || 
                            (a.IataCode != null && a.IataCode.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Select(a => new { a.IataCode, a.Name, a.City, a.Country })
                .Take(10)
                .ToList();

            Console.WriteLine($"[AirportsController] Found {matches.Count} matches.");
            return Ok(matches);
        }
    }
}

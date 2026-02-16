using System;
using System.Collections.Generic;
using System.Linq;
using FlightOptimizer.Core.DTOs;
using FlightOptimizer.Core.Entities;
using FlightOptimizer.Core.Interfaces;
using NetTopologySuite.Geometries;

namespace FlightOptimizer.Infrastructure.Services
{
    public class GraphEngine : IGraphEngine
    {
        // Graph Structure: SourceIATA -> List of Routes departing from there
        private Dictionary<string, List<Route>> _adjacencyList;
        private Dictionary<string, Airport> _airportCache;
        private List<RestrictedZone> _restrictedZones;

        public GraphEngine()
        {
            _adjacencyList = new Dictionary<string, List<Route>>();
            _airportCache = new Dictionary<string, Airport>();
            _restrictedZones = new List<RestrictedZone>();
        }

        public void Initialize(IEnumerable<Airport> airports, IEnumerable<Route> routes, IEnumerable<RestrictedZone> zones)
        {
            _restrictedZones = zones.ToList();
            _airportCache = airports.ToDictionary(a => a.IataCode, StringComparer.OrdinalIgnoreCase);
            
            // Build Adjacency List
            // We need to map Route.SourceAirportId to IATA
            // The passed 'routes' might have null SourceAirport/DestAirport because we loaded them asNoTracking or similar.
            // We should rely on the IDs and the _airportCache (which we just built from 'airports').
            
            var idToIataMap = airports.ToDictionary(a => a.Id, a => a.IataCode);

            foreach (var route in routes)
            {
                if (idToIataMap.TryGetValue(route.SourceAirportId, out string sourceIata) &&
                    idToIataMap.TryGetValue(route.DestAirportId, out string destIata))
                {
                    // Validate Restricted Zones for this edge
                    if (_airportCache.TryGetValue(sourceIata, out var sourceAirport) && 
                        _airportCache.TryGetValue(destIata, out var destAirport))
                    {
                        if (IsRouteSafe(sourceAirport, destAirport))
                        {
                            if (!_adjacencyList.ContainsKey(sourceIata))
                            {
                                _adjacencyList[sourceIata] = new List<Route>();
                            }
                            
                            // Rehydrate the route object with the airport objects for later use if needed, 
                            // or just trust the IDs. The algorithm just needs weights.
                            // But for reconstruction, we need IATAs.
                            // Let's create a lightweight or just attach the IATAs/Airports to a DTO if we were strict. 
                            // For now, attaching the FULL airport object to the route might be heavy if we have many references, 
                            // but 60k is fine. 
                            route.SourceAirport = sourceAirport;
                            route.DestAirport = destAirport;
                            
                            _adjacencyList[sourceIata].Add(route);
                        }
                    }
                }
            }
        }

        public PathResult FindPath(string source, string dest, RouteCriteria criteria)
        {
            var result = new PathResult();

            // Basic Validation
            if (!_airportCache.ContainsKey(source) || !_airportCache.ContainsKey(dest))
            {
                result.Success = false;
                result.Reason = FailureReason.NoRoute;
                result.Message = "Source or Destination airport not found.";
                return result;
            }

            // Dijkstra Algorithm
            var pq = new PriorityQueue<string, double>();
            var costs = new Dictionary<string, double>();
            var previous = new Dictionary<string, Route>();
            var visited = new HashSet<string>();

            // Initialize
            foreach (var node in _adjacencyList.Keys) costs[node] = double.MaxValue;
            costs[source] = 0;
            pq.Enqueue(source, 0);

            while (pq.Count > 0)
            {
                // 1. Pop cheapest node
                if (!pq.TryDequeue(out string u, out double currentCost)) break;
                
                // 2. Target check
                if (u == dest) break; // Found it!

                // 3. Visited check (lazy deletion handling)
                // If we found a better path to u already processed, skip? 
                // Standard Dijkstra: if we popped it, it is finalized.
                if (currentCost > costs.GetValueOrDefault(u, double.MaxValue)) continue;

                // 4. Neighbors
                if (_adjacencyList.TryGetValue(u, out var edges))
                {
                    foreach (var edge in edges)
                    {
                        string v = edge.DestAirport.IataCode; // We hydrated this in Initialize
                        
                        // Calculate weight based on criteria
                        double weight = criteria switch
                        {
                            RouteCriteria.Cheapest => (double)edge.Price,
                            RouteCriteria.Fastest => edge.DurationMinutes,
                            RouteCriteria.Layover => 1.0, // 1 hop = 1 cost
                            _ => (double)edge.Price
                        };

                        double newCost = currentCost + weight;
                        
                        if (newCost < costs.GetValueOrDefault(v, double.MaxValue))
                        {
                            costs[v] = newCost;
                            previous[v] = edge;
                            pq.Enqueue(v, newCost);
                        }
                    }
                }
            }

            // Reconstruction
            if (!previous.ContainsKey(dest))
            {
                // Path finding failed. Check for restricted zones blocking direct path.
                var srcAp = _airportCache[source];
                var dstAp = _airportCache[dest];
                if (!IsRouteSafe(srcAp, dstAp))
                {
                    result.Success = false;
                    result.Reason = FailureReason.RestrictedZoneBlock;
                    result.Message = "Direct route blocked by Restricted Zone.";
                    return result;
                }

                result.Success = false;
                result.Reason = FailureReason.NoRoute;
                result.Message = "No route found.";
                return result;
            }

            // Build Result
            var segments = new List<FlightSegment>();
            var curr = dest;

            while (curr != source)
            {
                var edge = previous[curr];
                
                var segment = new FlightSegment
                {
                    SourceCode = edge.SourceAirport.IataCode,
                    SourceName = edge.SourceAirport.Name,
                    SourceCountry = edge.SourceAirport.Country,
                    SourceLatitude = edge.SourceAirport.Latitude,
                    SourceLongitude = edge.SourceAirport.Longitude,
                    
                    DestCode = edge.DestAirport.IataCode,
                    DestName = edge.DestAirport.Name,
                    DestCountry = edge.DestAirport.Country,
                    DestLatitude = edge.DestAirport.Latitude,
                    DestLongitude = edge.DestAirport.Longitude,
                    
                    Price = (double)edge.Price,
                    DurationMinutes = edge.DurationMinutes
                };
                
                segments.Add(segment);
                curr = edge.SourceAirport.IataCode;
            }

            segments.Reverse(); // Reverse to get Source -> Dest order

            result.Success = true;
            result.Segments = segments;
            result.TotalPrice = segments.Sum(s => s.Price);
            result.TotalDuration = segments.Sum(s => s.DurationMinutes);
            result.TotalStops = Math.Max(0, segments.Count - 1);
            
            return result;
        }

        private bool IsRouteSafe(Airport source, Airport dest)
        {
            var geometryFactory = new GeometryFactory();
            var coords = new Coordinate[] 
            {
                new Coordinate(source.Longitude, source.Latitude),
                new Coordinate(dest.Longitude, dest.Latitude)
            };
            var routeLine = geometryFactory.CreateLineString(coords);

            foreach (var zone in _restrictedZones)
            {
                if (routeLine.Intersects(zone.Region))
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerable<RestrictedZone> GetRestrictedZones() => _restrictedZones;
        public IEnumerable<Airport> GetAirports() => _airportCache.Values;
    }
}

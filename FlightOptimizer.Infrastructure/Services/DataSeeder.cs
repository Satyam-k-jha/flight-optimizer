using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlightOptimizer.Core.Entities;
using FlightOptimizer.Infrastructure.Data;
using NetTopologySuite.Geometries;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FlightOptimizer.Infrastructure.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        // Regex for CSV splitting (handles quotes)
        private static readonly Regex CsvRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled);

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Path Setup
            string basePath = Path.Combine(Directory.GetCurrentDirectory(), "ExternalData");
            string airportsPath = Path.Combine(basePath, "airports.dat");
            string routesPath = Path.Combine(basePath, "routes.dat");

            Console.WriteLine($"[DataSeeder] Looking for files at: {basePath}");

            // --- FIX START: PREVENT DUPLICATE SEEDING CRASH ---
            int currentAirportCount = await _context.Airports.CountAsync();

            // 1. If we have a lot of data (e.g. > 10,000), assume DB is healthy and STOP.
            if (currentAirportCount > 10000)
            {
                Console.WriteLine($"[DataSeeder] Database already contains {currentAirportCount} airports. Skipping seed to prevent duplicates. 🚀");
                return; // <--- EXIT HERE so we don't try to insert GKA again
            }

            // 2. If we have partial data (some but not all), wipe it clean to avoid conflicts.
            if (currentAirportCount > 0)
            {
                Console.WriteLine($"[DataSeeder] Found partial/old data ({currentAirportCount}). Wiping database for a clean seed...");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Routes");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Airports");
            }
            // --- FIX END ---

            // Seed Restricted Zones if empty
            if (!await _context.RestrictedZones.AnyAsync())
            {
                var ukrainePoly = new Polygon(new LinearRing(new Coordinate[] {
                    new Coordinate(30, 45), new Coordinate(35, 45), new Coordinate(35, 50), new Coordinate(30, 50), new Coordinate(30, 45)
                })) { SRID = 4326 };
                var nkPoly = new Polygon(new LinearRing(new Coordinate[] {
                    new Coordinate(125, 35), new Coordinate(130, 35), new Coordinate(130, 40), new Coordinate(125, 40), new Coordinate(125, 35)
                })) { SRID = 4326 };

                var zones = new List<RestrictedZone>
                {
                    new RestrictedZone { Name = "War Zone A", Region = ukrainePoly },
                    new RestrictedZone { Name = "No Fly Zone B", Region = nkPoly }
                };
                await _context.RestrictedZones.AddRangeAsync(zones);
                await _context.SaveChangesAsync();
            }

            // 2. Parse & Seed Airports
            var airportDict = new Dictionary<string, Airport>();
            if (File.Exists(airportsPath))
            {
                Console.WriteLine($"[DataSeeder] Streaming {airportsPath}...");
                
                var newAirports = new List<Airport>();
                int processed = 0;

                // Use ReadLines for streaming
                foreach (var line in File.ReadLines(airportsPath))
                {
                    try
                    {
                        var parts = CsvRegex.Split(line);
                        // Schema expectations: 
                        // Index 1=Name, 2=City, 3=Country, 4=IATA, 6=Lat, 7=Lon
                        if (parts.Length < 8) continue;

                        string iata = Clean(parts[4]);
                        if (string.IsNullOrWhiteSpace(iata) || iata.Length != 3 || iata == "\\N") continue;
                        if (airportDict.ContainsKey(iata)) continue; // Dupe check

                        if (!double.TryParse(parts[6], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat)) continue;
                        if (!double.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon)) continue;

                        var airport = new Airport
                        {
                            Name = Clean(parts[1]),
                            City = Clean(parts[2]),
                            Country = Clean(parts[3]),
                            IataCode = iata,
                            Latitude = lat,
                            Longitude = lon
                        };

                        newAirports.Add(airport);
                        airportDict[iata] = airport; // Add to lookup
                        processed++;

                        // Batch Insert
                        if (newAirports.Count >= 2000)
                        {
                            await _context.Airports.AddRangeAsync(newAirports);
                            await _context.SaveChangesAsync();
                            newAirports.Clear();
                            Console.Write($"\r[DataSeeder] Saved {processed} airports...");
                        }
                    }
                    catch 
                    { 
                        // Skip bad lines silently or log debug
                    }
                }

                // Final Batch
                if (newAirports.Count > 0)
                {
                    await _context.Airports.AddRangeAsync(newAirports);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"\r[DataSeeder] Saved {processed} airports. Done.");
                }
            }
            else
            {
                Console.WriteLine($"[DataSeeder] ERROR: {airportsPath} not found.");
                return; // Cannot proceed without airports
            }

            // 3. Parse & Seed Routes
            // We need the IDs of the airports we just inserted to link them. 
            // Since we still have `airportDict` with the distinct objects, and EF Core populates IDs on SaveChanges,
            // the `airportDict` values SHOULD have IDs now.
            
            if (File.Exists(routesPath))
            {
                Console.WriteLine($"[DataSeeder] Streaming {routesPath}...");
                var newRoutes = new List<Route>();
                int processed = 0;
                var random = new Random();

                foreach (var line in File.ReadLines(routesPath))
                {
                    try
                    {
                        var parts = CsvRegex.Split(line);
                        // Schema: 2=Source, 4=Dest, 7=Stops
                        if (parts.Length < 8) continue;

                        string sourceIata = Clean(parts[2]);
                        string destIata = Clean(parts[4]);
                        string stops = Clean(parts[7]);

                        // Validation
                        if (stops != "0") continue;
                        if (!airportDict.TryGetValue(sourceIata, out var sourceAirport)) continue;
                        if (!airportDict.TryGetValue(destIata, out var destAirport)) continue;

                        // Calculations
                        double distanceKm = GetDistance(sourceAirport.Latitude, sourceAirport.Longitude, destAirport.Latitude, destAirport.Longitude);
                        double duration = (distanceKm / 900.0) * 60 + 45; // 900km/h + 45m taxi
                        
                        // Price Calculation: (Dist * 0.12) * (0.8 + 0.0-0.4 variance)
                        double variance = 0.8 + (random.NextDouble() * 0.4);
                        decimal price = (decimal)(distanceKm * 0.12 * variance);

                        var route = new Route
                        {
                            SourceAirportId = sourceAirport.Id,
                            DestAirportId = destAirport.Id,
                            SourceAirport = null!, // Avoid re-inserting navigation prop
                            DestAirport = null!,
                            AirlineCode = Clean(parts[0]),
                            Stops = 0,
                            Price = Math.Round(price, 2),
                            DurationMinutes = Math.Round(duration, 0)
                        };

                        newRoutes.Add(route);
                        processed++;

                        // Batch Insert
                        if (newRoutes.Count >= 2000)
                        {
                            await _context.Routes.AddRangeAsync(newRoutes);
                            await _context.SaveChangesAsync();
                            newRoutes.Clear();
                            Console.Write($"\r[DataSeeder] Saved {processed} routes...");
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                if (newRoutes.Count > 0)
                {
                    await _context.Routes.AddRangeAsync(newRoutes);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"\r[DataSeeder] Saved {processed} routes. Done.");
                }
            }
            else
            {
                Console.WriteLine($"[DataSeeder] ERROR: {routesPath} not found.");
            }
        }

        private string Clean(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Trim('"', ' ', '\t');
        }

        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; 
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}
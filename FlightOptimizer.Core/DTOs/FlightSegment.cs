namespace FlightOptimizer.Core.DTOs
{
    public class FlightSegment
    {
        public string SourceCode { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public string SourceCountry { get; set; } = string.Empty;
        public double SourceLatitude { get; set; }
        public double SourceLongitude { get; set; }
        
        public string DestCode { get; set; } = string.Empty;
        public string DestName { get; set; } = string.Empty;
        public string DestCountry { get; set; } = string.Empty;
        public double DestLatitude { get; set; }
        public double DestLongitude { get; set; }
        
        public double Price { get; set; }
        public double DurationMinutes { get; set; }
    }
}

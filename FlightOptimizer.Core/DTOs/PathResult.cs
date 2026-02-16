using FlightOptimizer.Core.DTOs;

namespace FlightOptimizer.Core.DTOs
{
    public class PathResult
    {
        public bool Success { get; set; }
        public List<FlightSegment> Segments { get; set; } = new List<FlightSegment>();
        public double TotalPrice { get; set; }
        public double TotalDuration { get; set; }
        public int TotalStops { get; set; }
        public FailureReason Reason { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public enum FailureReason
    {
        None,
        NoRoute,
        RestrictedZoneBlock
    }
}

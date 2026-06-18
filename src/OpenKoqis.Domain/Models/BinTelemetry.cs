namespace OpenKoqis.Domain.Models
{
    public class BinTelemetry
    {
        public int FillLevel { get; set; } // %
        public bool IsSmokeDetected { get; set; }
        public bool IsOverloaded { get; set; } // через край
        public DateTime LastUpdated { get; set; }
    }
}

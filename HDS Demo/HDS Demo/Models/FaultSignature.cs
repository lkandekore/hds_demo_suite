using System;
using System.Text.Json.Serialization;

namespace HDS_Demo.Models
{
    public class FaultSignature
    {
        public string ApplicationName { get; set; } = string.Empty;
        public Guid FaultId { get; set; } = Guid.NewGuid();

        public string FaultCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public CaptureRequest CaptureRequest { get; set; }

        // SERVER-ONLY FIELDS
        [JsonIgnore] public string TypeDescription { get; set; } = string.Empty;
        [JsonIgnore] public int Count { get; set; } = 1;
        [JsonIgnore] public DateTime LastTimestamp { get; set; }
        public FaultTimeSeries TimeSeries { get; set; } = new FaultTimeSeries();
        [JsonIgnore] public string PackageFile { get; set; } = string.Empty;
    }
}

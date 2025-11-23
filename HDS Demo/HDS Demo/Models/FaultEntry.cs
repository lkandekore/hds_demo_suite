namespace HDS_Demo.Models
{
    public class FaultEntry
    {
        public string FaultCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;

        // The description sent by the app
        public string Description { get; set; } = string.Empty;

        // The *type description* from lookup table (F0 = software exception, etc.)
        public string TypeDescription { get; set; } = string.Empty;

        // How many times this fault appeared
        public int Count { get; set; } = 1;

        // Latest timestamp seen
        public DateTime LastTimestamp { get; set; }
    }
}

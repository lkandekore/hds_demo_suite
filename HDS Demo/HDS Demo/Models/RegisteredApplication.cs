namespace HDS_Demo.Models
{
    public class RegisteredApplication
    {
        public Guid RegistrationId { get; set; } = Guid.NewGuid();

        public string Application { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public DateTime RegisteredUtc { get; set; } = DateTime.UtcNow;

        // For “last seen”
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    }
}

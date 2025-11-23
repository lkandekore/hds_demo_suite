using HDS_Demo.Models;

namespace HDS_Demo.Server
{
    public class ApplicationRegistry
    {
        private readonly List<RegisteredApplication> _apps = new();

        // Event for UI
        public event Action<RegisteredApplication>? ApplicationRegistered;

        public RegisteredApplication Register(string appName, string version, string registrationId)
        {
            var app = new RegisteredApplication
            {
                Application = appName,
                Version = version,
                RegisteredUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            };

            _apps.Add(app);

            // Fire event
            ApplicationRegistered?.Invoke(app);

            return app;
        }

        public List<RegisteredApplication> GetAll() => _apps;
    }
}

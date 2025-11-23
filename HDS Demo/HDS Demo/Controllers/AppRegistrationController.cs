using HDS_Demo.Server;
using Microsoft.AspNetCore.Mvc;

namespace HDS_Demo.Controllers
{
    [ApiController]
    [Route("api/v1/apps")]
    public class AppRegistrationController : ControllerBase
    {
        private readonly ApplicationRegistry _registry;

        public AppRegistrationController(ApplicationRegistry registry)
        {
            _registry = registry;
        }

        // POST /api/v1/apps/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegistrationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Application) ||
                string.IsNullOrWhiteSpace(request.Version))
            {
                return BadRequest(new { error = "application and version are required" });
            }

            var result = _registry.Register(request.Application, request.Version, request.RegistrationId);

            return Ok(new
            {
                status = "registered",
                application = result.Application,
                version = result.Version,
                registered = result.RegisteredUtc,
                last_seen = result.LastSeenUtc
            });
        }

        // GET /api/v1/apps
        [HttpGet]
        public IActionResult GetAll()
        {
            var list = _registry
                .GetAll()
                .Select(a => new
                {
                    application = a.Application,
                    version = a.Version,
                    registration_id = a.RegistrationId.ToString(),
                    registered = a.RegisteredUtc,
                    last_seen = a.LastSeenUtc
                })
                .ToList();

            return Ok(new
            {
                count = list.Count,
                applications = list
            });
        }


        public class RegistrationRequest
        {
            public string Application { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
            public string RegistrationId { get; set; } = string.Empty;
        }
    }
}

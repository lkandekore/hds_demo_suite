using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HDS_Demo.Server
{
    public class HdsServer
    {
        private WebApplication? _app;

        public IServiceProvider? Services => _app?.Services;

        public async Task StartAsync(int port = 5005)
        {
            if (_app != null)
                return;

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls($"http://localhost:{port}");

            builder.Services.AddSingleton<ApplicationRegistry>();
            builder.Services.AddSingleton<FaultRegistry>();
            builder.Services.AddSingleton<HdsViewModel>();
            builder.Services.AddControllers();

            _app = builder.Build();
            _app.MapControllers();

            await _app.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_app != null)
            {
                await _app.StopAsync();
                _app = null;
            }
        }
    }
}

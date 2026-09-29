using System.Runtime.Versioning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dust2Agent.Service;

/// <summary>
/// Windows o'chayotganda xizmat SERVICE_CONTROL_SHUTDOWN oladi. Buni belgilab
/// qo'yamiz — shunda oddiy o'chirish "xizmat to'xtatildi" ogohlantirishiga aylanmaydi.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class AgentServiceLifetime : WindowsServiceLifetime
{
    public AgentServiceLifetime(
        IHostEnvironment environment,
        IHostApplicationLifetime applicationLifetime,
        ILoggerFactory loggerFactory,
        IOptions<HostOptions> optionsAccessor,
        IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor)
        : base(environment, applicationLifetime, loggerFactory, optionsAccessor, windowsServiceOptionsAccessor)
    {
    }

    protected override void OnShutdown()
    {
        SystemShutdown.Mark();
        base.OnShutdown();
    }
}

using Dust2Agent.Common;
using Dust2Agent.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

AgentPaths.EnsureCreated();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(new AgentLog("service"));
builder.Services.AddHostedService<AgentWorker>();

// Windows xizmati sifatida ham, konsoldan ham ishga tushadi (sinov uchun qulay)
builder.Services.AddWindowsService(options => options.ServiceName = "Dust2Agent");

var host = builder.Build();
await host.RunAsync();

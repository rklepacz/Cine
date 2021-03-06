using System;
using System.Linq;
using System.Threading.Tasks;
using Convey;
using Convey.CQRS.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cine.Shared.Modules
{
    public static class Extensions
    {
        public static IConveyBuilder AddModuleRequests(this IConveyBuilder builder)
        {
            builder.AddModuleRegistry();
            builder.Services.AddSingleton<IModuleSubscriber, ModuleSubscriber>();
            builder.Services.AddTransient<IModuleClient, ModuleClient>();

            return builder;
        }

        public static IModuleSubscriber UseModuleRequests(this IApplicationBuilder app)
            => app.ApplicationServices.GetRequiredService<IModuleSubscriber>();

        private static void AddModuleRegistry(this IConveyBuilder builder)
        {
            var eventTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => a.FullName.Contains("Cine"))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && typeof(IEvent).IsAssignableFrom(t))
                .ToList();

            builder.Services.AddSingleton<IModuleRegistry>(provider =>
            {
                var logger = provider.GetService<ILogger<IModuleRegistry>>();
                var registry = new ModuleRegistry(logger);

                foreach (var type in eventTypes)
                {
                    registry.AddBroadcastAction(type, (sp, @event) =>
                    {
                        var dispatcher = sp.GetService<IEventDispatcher>();
                        return (Task)dispatcher.GetType()
                            .GetMethod(nameof(dispatcher.PublishAsync))
                            .MakeGenericMethod(type)
                            .Invoke(dispatcher, new[] {@event});
                    });
                }

                return registry;
            });
        }
    }
}

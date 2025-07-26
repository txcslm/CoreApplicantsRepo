using ChatSystem.Application.Handlers;
using ChatSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Application;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddApplicationServices(this IServiceCollection services)
  {
    services.AddSingleton<IMediator, Mediator>();
    services.AddTransient<SendMessageCommandHandler>();

    return services;
  }
}
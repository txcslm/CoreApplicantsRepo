using ChatSystem.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChatSystem.Presentation;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddPresentationServices(this IServiceCollection services)
  {
    services.AddSingleton<ChatManager>();

    return services;
  }
}
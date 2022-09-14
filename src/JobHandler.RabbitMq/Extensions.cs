using JobHandler.Executor;
using JobHandler.RabbitMq.Executor;
using JobHandler.RabbitMq.Sender;
using JobHandler.Sender;
using Microsoft.Extensions.DependencyInjection;

namespace JobHandler.RabbitMq;

public static class Extensions
{
    public static IServiceCollection AddRabbitMqJobSender(this IServiceCollection serviceCollection,
        Action<RabbitMqSenderConfiguration> configBuilder)
    {
        serviceCollection.AddSingleton<ISender>(new RabbitMqSender(configBuilder));
        return serviceCollection;
    }

    public static IServiceCollection AddRabbitMqJobExecutor<T>(this IServiceCollection serviceCollection,
        Action<RabbitMqExecutorConfiguration> configBuilder)
    {
        serviceCollection.AddSingleton<IExecutor<T>>(new RabbitMqExecutor<T>(configBuilder));
        return serviceCollection;
    }
}
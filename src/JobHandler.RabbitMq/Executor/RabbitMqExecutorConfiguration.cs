using JobHandler.Executor;

namespace JobHandler.RabbitMq.Executor;

public class RabbitMqExecutorConfiguration : ExecutorConfiguration
{
    public string HostName { get; set; } = "localhost";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
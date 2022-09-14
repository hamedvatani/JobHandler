using JobHandler.Sender;

namespace JobHandler.RabbitMq.Sender;

public class RabbitMqSenderConfiguration : SenderConfiguration
{
    public string HostName { get; set; } = "localhost";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}
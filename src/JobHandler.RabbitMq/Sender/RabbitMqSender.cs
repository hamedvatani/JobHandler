using System.Text;
using JobHandler.Sender;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace JobHandler.RabbitMq.Sender;

public class RabbitMqSender : ISender
{
    private readonly RabbitMqSenderConfiguration _configuration;
    private ConnectionFactory _factory = null!;
    private IConnection _connection = null!;
    private IModel _channel = null!;

    public RabbitMqSender(RabbitMqSenderConfiguration configuration)
    {
        _configuration = configuration;
        Setup();
    }

    public RabbitMqSender(Action<RabbitMqSenderConfiguration> configBuilder)
    {
        _configuration = new RabbitMqSenderConfiguration();
        configBuilder(_configuration);
        Setup();
    }

    private void Setup()
    {
        _factory = new ConnectionFactory
        {
            HostName = _configuration.HostName,
            UserName = _configuration.UserName,
            Password = _configuration.Password
        };
        _connection = _factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(_configuration.GroupName, _configuration.Durable, false, false);
    }

    public void Send<T>(T job)
    {
        var str = JsonConvert.SerializeObject(job);
        var bytes = Encoding.UTF8.GetBytes(str);
        var props = _channel.CreateBasicProperties();
        props.Persistent = _configuration.Durable;
        props.Headers = new Dictionary<string, object>();
        props.Headers.Add("Timeout", _configuration.Timeout);
        props.Headers.Add("Retries", 0);
        props.Headers.Add("MaxRetries", _configuration.MaxRetries);
        _channel.BasicPublish("", _configuration.GroupName, props, bytes);
    }

    public Task SendAsync<T>(T job)
    {
        return Task.Run(() => Send(job));
    }
}
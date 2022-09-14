using System.Text;
using JobHandler.Executor;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JobHandler.RabbitMq.Executor;

public class RabbitMqExecutor<T> : IExecutor<T>
{
    private readonly RabbitMqExecutorConfiguration _configuration;
    private ConnectionFactory _factory = null!;
    private IConnection _connection = null!;
    private IModel _channel = null!;
    private EventingBasicConsumer _consumer = null!;
    private string _consumerTag = "";
    private string FailJobsQueue => _configuration.GroupName + "_FailJobs";
    private string RetryJobsExchange => _configuration.GroupName + "_RetryJobs";

    public RabbitMqExecutor(RabbitMqExecutorConfiguration configuration)
    {
        _configuration = configuration;
        Setup();
    }

    public RabbitMqExecutor(Action<RabbitMqExecutorConfiguration> configBuilder)
    {
        _configuration = new RabbitMqExecutorConfiguration();
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
        _channel.QueueDeclare(FailJobsQueue, true, false, false);
        _channel.ExchangeDeclare(RetryJobsExchange, "x-delayed-message", true, false,
            new Dictionary<string, object> {{"x-delayed-type", "direct"}});
        _channel.QueueBind(queue: _configuration.GroupName, exchange: RetryJobsExchange, routingKey: "");
        _channel.BasicQos(0, _configuration.MaxThreads, false);
        _consumer = new EventingBasicConsumer(_channel);
    }

    public void StartExecution(Func<T, CancellationToken, FuncResult> executor, Action<T, List<FuncResult>>? failAction)
    {
        _consumer.Received += async (_, ea) =>
        {
            var bytes = ea.Body.ToArray();
            var str = Encoding.UTF8.GetString(bytes);
            T? job = default(T);
            try
            {
                job = JsonConvert.DeserializeObject<T>(str);
                if (job == null)
                    FailJob(ea, FuncResult.Fail("Cant parse job"), failAction, job);
                else
                {
                    var timeout = (int) ea.BasicProperties.Headers["Timeout"];
                    var retries = (int) ea.BasicProperties.Headers["Retries"];
                    var maxRetries = (int) ea.BasicProperties.Headers["MaxRetries"];
                    var source = new CancellationTokenSource(timeout);
                    var task = Task.Run(() => executor(job, source.Token), source.Token);
                    var t = await Task.WhenAny(task, Task.Delay(timeout, source.Token));
                    retries++;
                    ea.BasicProperties.Headers["Retries"] = retries;
                    if (task == t)
                    {
                        if (!task.Result.IsSuccess)
                        {
                            if (retries >= maxRetries)
                                FailJob(ea, task.Result, failAction, job);
                            else
                                RetryJob(ea, task.Result);
                        }
                    }
                    else
                    {
                        if (retries >= maxRetries)
                            FailJob(ea, FuncResult.Timeout(), failAction, job);
                        else
                            RetryJob(ea, FuncResult.Timeout());
                    }
                }
            }
            catch (Exception ex)
            {
                FailJob(ea, FuncResult.Fail(ex.Message), failAction, job);
            }
            finally
            {
                _channel.BasicAck(ea.DeliveryTag, false);
            }
        };
        _consumerTag = _channel.BasicConsume(_configuration.GroupName, false, _consumer);
    }

    public void StopExecution()
    {
        if (!string.IsNullOrEmpty(_consumerTag))
            _channel.BasicCancel(_consumerTag);
    }

    private void RetryJob(BasicDeliverEventArgs ea, FuncResult result)
    {
        var props = ea.BasicProperties;
        if (props.Headers.ContainsKey("x-delay"))
            props.Headers["x-delay"] = _configuration.RetryDelay;
        else
            props.Headers.Add("x-delay", _configuration.RetryDelay);
        List<FuncResult> list = new();
        if (!props.Headers.ContainsKey("Results"))
            props.Headers.Add("Results", null);
        else
            list = DeserializeFuncResultList((byte[]) props.Headers["Results"]);
        list.Add(result);
        props.Headers["Results"] = SerializeFuncResultList(list);
        _channel.BasicPublish(RetryJobsExchange, "", props, ea.Body);
    }

    private void FailJob(BasicDeliverEventArgs ea, FuncResult result, Action<T, List<FuncResult>>? failAction, T? job)
    {
        var props = ea.BasicProperties;
        props.Persistent = true;
        props.Headers ??= new Dictionary<string, object>();
        List<FuncResult> list = new();
        if (!props.Headers.ContainsKey("Results"))
            props.Headers.Add("Results", null);
        else
            list = DeserializeFuncResultList((byte[]) props.Headers["Results"]);
        list.Add(result);
        props.Headers["Results"] = SerializeFuncResultList(list);
        if (failAction == null || job == null)
            _channel.BasicPublish("", FailJobsQueue, props, ea.Body);
        else
            failAction(job, list);
    }

    private byte[] SerializeFuncResultList(List<FuncResult> results)
    {
        var str = JsonConvert.SerializeObject(results);
        return Encoding.UTF8.GetBytes(str);
    }

    private List<FuncResult> DeserializeFuncResultList(byte[] data)
    {
        var str = Encoding.UTF8.GetString(data);
        return JsonConvert.DeserializeObject<List<FuncResult>>(str) ?? new();
    }
}
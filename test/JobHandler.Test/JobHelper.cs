using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JobHandler.Executor;
using JobHandler.RabbitMq.Executor;
using JobHandler.RabbitMq.Sender;
using JobHandler.Sender;
using RabbitMQ.Client;

namespace JobHandler.Test;

public class JobHelper
{
    public ConcurrentBag<Job> ReceivedJobs { get; set; } = new();
    public Job FailJob { get; set; } = null!;
    public List<FuncResult> FailJobErrorList { get; set; } = null!;

    private readonly ISender _sender;
    private readonly IExecutor<Job> _executor;

    public JobHelper(string groupName, ushort maxThreads = 1, int timeout = 10000, int maxRetries = 3)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDelete(groupName);
            channel.QueueDelete(groupName + "_FailJobs");
            channel.ExchangeDelete(groupName + "_RetryJobs");
        }

        _sender = new RabbitMqSender(c =>
        {
            c.GroupName = groupName;
            c.Timeout = timeout;
            c.MaxRetries = maxRetries;
        });
        _executor = new RabbitMqExecutor<Job>(c =>
        {
            c.GroupName = groupName;
            c.MaxThreads = maxThreads;
        });
    }

    public void Send(Job job)
    {
        job.PublishTime = DateTime.Now;
        _sender.Send(job);
    }

    public void Send(string job)
    {
        _sender.Send(job);
    }

    public void Start(int jobDelay = 500, bool returnValue = true, bool useFailAction = false)
    {
        _executor.StartExecution(
            (job, token) =>
            {
                job.ConsumeTime = DateTime.Now;
                Thread.Sleep(jobDelay);
                job.ExecuteTime = DateTime.Now;
                if (!token.IsCancellationRequested)
                    ReceivedJobs.Add(job);
                return new FuncResult {IsSuccess = returnValue};
            }
            , useFailAction
                ? (job, errorList) =>
                {
                    FailJob = job;
                    FailJobErrorList = errorList;
                }
                : null);
    }

    public void Stop()
    {
        _executor.StopExecution();
    }
}
namespace JobHandler.Executor;

public abstract class ExecutorConfiguration
{
    public bool Durable { get; set; } = false;
    public string GroupName { get; set; } = "";
    public ushort MaxThreads { get; set; } = 1;
    public int RetryDelay { get; set; } = 5000;
}
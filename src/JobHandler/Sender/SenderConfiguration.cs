namespace JobHandler.Sender;

public abstract class SenderConfiguration
{
    public bool Durable { get; set; } = false;
    public string GroupName { get; set; } = "";
    public int Timeout { get; set; } = 10000;
    public int MaxRetries { get; set; } = 3;
}